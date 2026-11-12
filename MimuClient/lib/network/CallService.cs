using LocalMimu.Models;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;

public class CallService
{
    private NetworkService? _net { get; set; }
    private Guid _myUserId = Guid.Empty;
    private bool _forceTurnRelay;
    public CallState currentState { get; set; }
    public Guid CurrentCallId;
    public Guid PeerUserId;
    List<string> _earlyIceCandidates = new();
    RTCPeerConnection? _peerConn;
    public bool isRemoteRelayEnabled;
    public string? _pendingOfferSdp;
    public Action<CallState>? OnStateChanged;
    public Action<Guid, Guid>? OnIncomingCall;
    public Action<string>? OnCallEnded;
    public CallService(NetworkService? service, Guid myUserId, bool forceTurnRelay)
    {
        _net = service;
        _myUserId = myUserId;
        _forceTurnRelay = forceTurnRelay;
        currentState = CallState.Idle;
        _net.OnCallSignalReceived += HandleSignal;
    }

    private void Cleanup(string reason)
    {
        _peerConn?.Close(reason);
        _peerConn = null;

        CurrentCallId = Guid.Empty;
        PeerUserId = Guid.Empty;
        _pendingOfferSdp = null;
        isRemoteRelayEnabled = false;
        _earlyIceCandidates.Clear();

        currentState = CallState.Idle;
        OnStateChanged?.Invoke(currentState);
        OnCallEnded?.Invoke(reason);
    }

    private void InitPeerConnection(TurnCredentials? creds)
    {
        var rtcConf = new RTCConfiguration();
        if (creds != null && !string.IsNullOrWhiteSpace(creds.TurnUrl))
        {
            var rtcServ = new RTCIceServer
            {
                urls = creds.TurnUrl,
                username = creds.Username,
                credential = creds.Password
            };
            rtcConf.iceServers = new List<RTCIceServer> { rtcServ };
        }
        if (_forceTurnRelay)
        {
            rtcConf.iceTransportPolicy = RTCIceTransportPolicy.relay;
        }
        if (!_forceTurnRelay)
        {
            rtcConf.iceTransportPolicy = RTCIceTransportPolicy.all;
        }
        _peerConn = new RTCPeerConnection(rtcConf);

        var format = new SDPAudioVideoMediaFormat(SDPWellKnownMediaFormatsEnum.PCMU);
        var track = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPAudioVideoMediaFormat> { format }, MediaStreamStatusEnum.SendRecv);

        _peerConn.addTrack(track);
        _peerConn.onicecandidate += async cand =>
        {
            if (cand == null || CurrentCallId == Guid.Empty || PeerUserId == Guid.Empty)
            {
                return;
            }
            var callSig = new CallSignalPayload();
            callSig.CallId = CurrentCallId;
            callSig.TargetUserId = PeerUserId;
            callSig.SenderId = _myUserId;
            callSig.Data = cand.ToString();

            await _net.SendCallSignalAsync(PacketType.IceCandidate, callSig);
        };

        _peerConn.onconnectionstatechange += state =>
        {
            if (state == RTCPeerConnectionState.connected)
            {
                currentState = CallState.InCall;
                OnStateChanged?.Invoke(currentState);
            }
            if (state == RTCPeerConnectionState.failed || state == RTCPeerConnectionState.closed)
            {
                Cleanup("CONNECTION_LOST");
            }
        };
    }

    public async Task StartCallAsync(Guid targetUserId)
    {
        if (currentState != CallState.Idle)
        {
            throw new InvalidOperationException();
        }
        var creds = await _net.GetTurnCredentialsAsync();
        CurrentCallId = Guid.NewGuid();
        PeerUserId = targetUserId;
        currentState = CallState.OutgoingRinging;
        InitPeerConnection(creds);

        var offer = _peerConn.createOffer();
        await _peerConn.setLocalDescription(offer);
        string sdpString = offer.sdp;

        var callSignal = new CallSignalPayload();
        callSignal.CallId = CurrentCallId;
        callSignal.SenderId = _myUserId;
        callSignal.Data = sdpString;
        callSignal.TargetUserId = targetUserId;

        await _net.SendCallSignalAsync(PacketType.CallOffer, callSignal);
    }
    public async Task AcceptCallAsync()
    {
        if (currentState != CallState.IncomingRinging)
        {
            throw new InvalidOperationException();
        }
        currentState = CallState.Connecting;
        var creds = await _net.GetTurnCredentialsAsync();
        InitPeerConnection(creds);

        var path = _peerConn.setRemoteDescription(new RTCSessionDescriptionInit
        {
            type = RTCSdpType.offer,
            sdp = _pendingOfferSdp
        });

        isRemoteRelayEnabled = true;

        foreach (var ice in _earlyIceCandidates)
        {
            _peerConn.addIceCandidate(new RTCIceCandidateInit
            {
                candidate = ice
            });
        }
        _earlyIceCandidates.Clear();
        var answer = _peerConn.createAnswer();
        await _peerConn.setLocalDescription(answer);
        string ansString = answer.sdp;

        var callSignal = new CallSignalPayload();
        callSignal.CallId = CurrentCallId; // ? по идее правильно 
        callSignal.SenderId = _myUserId;
        callSignal.Data = ansString;
        callSignal.TargetUserId = PeerUserId;

        await _net.SendCallSignalAsync(PacketType.CallAnswer, callSignal);

    }

    public async Task HangupAsync(string reason = "HANGUP")
    {
        if (currentState != CallState.Idle)
        {
            var hungUpSignal = new CallSignalPayload();
            hungUpSignal.CallId = CurrentCallId; // а вот тут не уверен 
            hungUpSignal.SenderId = _myUserId;
            hungUpSignal.TargetUserId = PeerUserId;
            hungUpSignal.Data = reason;
            await _net.SendCallSignalAsync(PacketType.CallHangup, hungUpSignal);

            Cleanup(reason);
        }
    }
    private void ApplyRemoteSdpAndFlushCandidates(string sdp, RTCSdpType sdpType)
    {
        if (_peerConn == null) return;

        _peerConn.setRemoteDescription(new RTCSessionDescriptionInit
        {
            type = sdpType,
            sdp = sdp
        });

        isRemoteRelayEnabled = true;

        foreach (var cand in _earlyIceCandidates)
        {
            _peerConn.addIceCandidate(new RTCIceCandidateInit { candidate = cand });
        }
        _earlyIceCandidates.Clear();
    }
    private async void HandleSignal(PacketType type, CallSignalPayload payload)
    {
        if (type == PacketType.CallOffer)
        {
            if (currentState != CallState.Idle)
            {
                var call = new CallSignalPayload();
                call.CallId = payload.CallId;
                call.TargetUserId = payload.SenderId;
                call.SenderId = _myUserId;
                call.Data = "BUSY";

                return;

            }
            CurrentCallId = payload.CallId;
            PeerUserId = payload.SenderId;
            _pendingOfferSdp = payload.Data;
            currentState = CallState.IncomingRinging;
            OnStateChanged?.Invoke(currentState);
            OnIncomingCall?.Invoke(payload.CallId, payload.SenderId);
            return;
        }
        if (type == PacketType.CallAnswer || type == PacketType.IceCandidate || type == PacketType.CallHangup)
        {
            if (payload.CallId != CurrentCallId)
            {
                return;
            }
            if (type == PacketType.CallAnswer)
            {
                currentState = CallState.Connecting;
                ApplyRemoteSdpAndFlushCandidates(payload.Data!, RTCSdpType.answer);
            }
            if (type == PacketType.CallHangup)
            {
                Cleanup(payload.Data ?? "REMOTE_HANGUP");
            }
            if (type == PacketType.IceCandidate)
            {
                if (isRemoteRelayEnabled)
                {
                    _peerConn.addIceCandidate(new RTCIceCandidateInit {candidate = payload.Data});
                }
                if (!isRemoteRelayEnabled)
                {
                    _earlyIceCandidates.Add(payload.Data!);
                }

            }
        }









    }



}