using Avalonia.Controls;
using Avalonia.Markup.Declarative;
using MimuGui.ViewModels;
using Avalonia.Data;
using Avalonia.Layout;
using System.Xml;
using System.Drawing;
using Avalonia.Styling;
using Avalonia.Media;
using Avalonia.Controls.Templates;
using System.Globalization;
using Avalonia.Data.Converters;
using System;
using Avalonia.Input;
using LocalMimu.Models;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Avalonia.Utilities;


namespace MimuGui.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;

    public MainWindow()
    {
        _vm = new MainWindowViewModel();
        _vm.StorageService = new WindowStorageService(this);
        if (_vm.StorageService == null)
        {
            Console.WriteLine("Иньекция НЕ прошла");
        }
        else
        {
            Console.WriteLine("Иньекция успешна");
        }
        _vm.AttachButtonText = $"📎 {_vm.InstanceId.ToString().Substring(0, 4)}";
        DataContext = _vm;
        BuildUI();
        _vm.ChatMessages.CollectionChanged += (s, e) =>
        {
            if (_vm.ChatMessages.Count == 0) return;

            var lastMsg = _vm.ChatMessages[^1];

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _chat.ScrollIntoView(lastMsg);
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        };
    }

    private class WindowStorageService : IStorageService
    {
        private readonly Window _window;
        public WindowStorageService(Window window)
        {
            _window = window;
        }
        public async Task<string?> PickFileAsync()
        {
            var storage = _window.StorageProvider;
            var files = await storage.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Выберите файл для отправки",
                AllowMultiple = false
            });
            if (files.Count >= 1)
            {
                return files[0].TryGetLocalPath();
            }
            return null;
        }
    }

    public ListBox _chat { get; set; }

    private void BuildUI()
    {
        _chat = new ListBox()
        {
            [Grid.RowProperty] = 1,
            Background = Brushes.Transparent,
            [!ListBox.ItemsSourceProperty] = new Binding(nameof(MainWindowViewModel.ChatMessages)),

            ItemTemplate = new FuncDataTemplate<Message>((msg, namescope) =>
            {
                var downloadBtn = new Button()
                {
                    Content = "Скачать",
                    Background = Brush.Parse("#3478F6"),
                    Foreground = Brushes.White,
                    Margin = Avalonia.Thickness.Parse("0, 5, 0, 0"),
                    CornerRadius = new Avalonia.CornerRadius(5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    [!Button.IsVisibleProperty] = new Binding("Type")
                    {
                        Converter = new Avalonia.Data.Converters.FuncValueConverter<MessageType, bool>(type => type == MessageType.File)
                    }
                };

                downloadBtn.Click += (sender, e) =>
                {
                    _ = _vm.DownloadFileAsync(msg);
                };

                return new Border()
                {
                    [!Border.BackgroundProperty] = new Binding("SenderID") { Converter = new BubbleColorConverter(_vm) },
                    CornerRadius = new Avalonia.CornerRadius(10),
                    Margin = Avalonia.Thickness.Parse("5"),
                    Padding = Avalonia.Thickness.Parse("10"),
                    [!Border.HorizontalAlignmentProperty] = new Binding("SenderID") { Converter = new MessageConvertor(_vm) },
                    Child = new StackPanel()
                    {
                        Spacing = 4,
                        Children =
                        {
                    new TextBlock()
                    {
                        [!TextBlock.TextProperty] = new Binding("DisplayText"),
                        Foreground = Brushes.White,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock()
                    {
                        [!TextBlock.TextProperty] = new Binding("SentAt") {Converter = new TimeConverter()},
                        FontSize = 10,
                        Foreground = Brush.Parse("#A0a0a0"),
                        HorizontalAlignment = HorizontalAlignment.Right,
                    },
                    new TextBlock()
                    {
                        [!TextBlock.TextProperty] = new Binding("Status") {Converter = new StatusConverter()},
                        FontSize = 10,
                        Foreground = Brush.Parse("#888888"),
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    downloadBtn
                        }
                    }
                };
            })
        };

        this.Width = 800;
        this.Height = 600;
        this.Title = "LocalMimu";

        this.Content = new Panel()
        {
            Children =
            {
                new StackPanel()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 18,
                    [!StackPanel.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsLoginVisible)),
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = "Авторизация в LocalMimu",
                            FontSize = 20,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBox()
                        {
                            Width = 300,
                            Watermark = "Введите username",
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.Username)) { Mode = BindingMode.TwoWay }
                        },
                        new TextBox()
                        {
                            Width = 300,
                            Watermark = "Введите пароль",
                            PasswordChar='*',
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.Password)) { Mode = BindingMode.TwoWay }
                        },
                        new Button()
                        {
                            Width = 150,
                            Height = 35,
                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.OnLogClicked)),
                            Content = "Войти",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            VerticalContentAlignment = VerticalAlignment.Center
                        },
                        new Button()
                        {
                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.SwitchToRegister)),
                            Width = 150,
                            Height = 35,
                            Content = "Регистрация",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            VerticalContentAlignment = VerticalAlignment.Center
                        },
                        new TextBlock()
                        {
                            [!TextBlock.TextProperty] = new Binding(nameof(MainWindowViewModel.StatusMessage)),
                            Foreground = Brush.Parse("#810505"),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 14
                        }
                    }
                },
                  new StackPanel()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 18,
                    [!StackPanel.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsRegisterVisible)),
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = "Регистрация в Mimu",
                            FontSize = 20,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBox()
                        {
                            Width = 300,
                            Watermark = "Введите отображаемое имя",
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.RegName)) { Mode = BindingMode.TwoWay }
                        },
                        new TextBox()
                        {
                            Width = 300,
                            Watermark = "Введите username",
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.RegUsername)) { Mode = BindingMode.TwoWay }
                        },
                        new TextBox()
                        {
                            Width = 300,
                            Watermark = "Введите пароль",
                            PasswordChar='*',
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.RegPassword)) { Mode = BindingMode.TwoWay }
                        },
                        new Button()
                        {
                            Width = 150,
                            Height = 35,
                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.OnRegisterClick)),
                            Content = "Зарегистрироваться",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            VerticalContentAlignment = VerticalAlignment.Center
                        },
                        new Button()
                        {
                            Content = "Войти",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.SwitchToLogin))
                        },
                        new TextBlock()
                        {
                            [!TextBlock.TextProperty] = new Binding(nameof(MainWindowViewModel.StatusMessage)),
                            Foreground = Brush.Parse("#810505"),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 14
                        }
                    }
                },
                new Grid()
                {
                    ColumnDefinitions = new ColumnDefinitions("250,*"),
                    Background = Brush.Parse("#121212"),
                    [!Grid.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsMainVisible)),
                    Children =
                    {
                        new Grid()
                        {
                            [Grid.ColumnProperty] = 0,
                            RowDefinitions = new RowDefinitions("Auto, *"),
                            Background = Brush.Parse("#141414"),
                            Children =
                            {
                                new Grid()
                                {
                                    [Grid.RowProperty] = 0,
                                    ColumnDefinitions = new ColumnDefinitions("*, Auto, Auto"),
                                    Margin = Avalonia.Thickness.Parse("10"),
                                    Children =
                                    {
                                        new TextBox()
                                        {
                                            [Grid.ColumnProperty] = 0,
                                            Watermark = "Поиск...",
                                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.SearchingText)) { Mode = BindingMode.TwoWay }
                                        },
                                        new Button()
                                        {
                                            [Grid.ColumnProperty] = 1,
                                            Content = "Найти",
                                            Margin = Avalonia.Thickness.Parse("5,0,0,0"),
                                            Background = Brush.Parse("#202020"),
                                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.SearchingUserAsync))
                                        },
                                        new Button()
                                        {
                                            [Grid.ColumnProperty] = 2,
                                            [!Button.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsSearchVisible)),
                                            Content = "X",
                                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.CancelSearch)),
                                            Background = Brushes.Transparent,
                                            Foreground = Brushes.White,
                                            CornerRadius = Avalonia.CornerRadius.Parse("18"),
                                            Margin = Avalonia.Thickness.Parse("5,0,0,0")
                                        }
                                    }
                                },
                                new ListBox()
                                {
                                    [Grid.RowProperty] = 1,
                                    Background = Brush.Parse("#141414"),
                                    Styles =
                                    {
                                        new Style(x => x.OfType<ListBoxItem>().Class(":pointerover"))
                                        { Setters = { new Setter(ListBoxItem.BackgroundProperty, Brush.Parse("#272727")) } },
                                        new Style(x => x.OfType<ListBoxItem>().Class(":selected"))
                                        { Setters = { new Setter(ListBoxItem.BackgroundProperty, Brush.Parse("#272727")) } }
                                    },
                                    [!ListBox.ItemsSourceProperty] = new Binding(nameof(MainWindowViewModel.ActiveChats)),
                                    [!ListBox.SelectedItemProperty] = new Binding(nameof(MainWindowViewModel.SelectedUser)) { Mode = BindingMode.TwoWay },
                                    ItemTemplate = new FuncDataTemplate<User>((user, namescope) =>
                                    {
                                        return new Border()
                                        {
                                            CornerRadius = Avalonia.CornerRadius.Parse("6"),
                                            Background = Brushes.Transparent,
                                            Child = new Grid()
                                            {
                                                ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto"),
                                                Margin = Avalonia.Thickness.Parse("0,6"),
                                                Children =
                                                {
                                                    new Border()
                                                    {
                                                        [Grid.ColumnProperty] = 0,
                                                        Width = 40,
                                                        Height = 40,
                                                        CornerRadius = new Avalonia.CornerRadius(20),
                                                        Background = Brush.Parse("#424141"),
                                                        VerticalAlignment = VerticalAlignment.Center,
                                                        Margin = Avalonia.Thickness.Parse("0,0,10,0")
                                                    },
                                                    new StackPanel()
                                                    {
                                                        [Grid.ColumnProperty] = 1,
                                                        VerticalAlignment = VerticalAlignment.Center,
                                                        Children =
                                                        {
                                                            new TextBlock()
                                                            {
                                                                [!TextBlock.TextProperty] = new Binding("Username"),
                                                                Foreground = Brushes.White,
                                                                FontWeight = FontWeight.Bold,
                                                                FontSize = 14
                                                            },
                                                            new TextBlock()
                                                            {
                                                                [!TextBlock.TextProperty] = new Binding("LastMessageText"),
                                                                Foreground = Brush.Parse("#888888"),
                                                                FontSize = 12,
                                                                MaxLines = 1,
                                                                TextTrimming = TextTrimming.CharacterEllipsis
                                                            }
                                                        }
                                                    },
                                                    new Border()
                                                    {
                                                        [Grid.ColumnProperty] = 2,
                                                        Background = Brush.Parse("#3478F6"),
                                                        CornerRadius = new Avalonia.CornerRadius(10),
                                                        MinWidth = 20,
                                                        Height = 20,
                                                        HorizontalAlignment = HorizontalAlignment.Right,
                                                        VerticalAlignment = VerticalAlignment.Center,
                                                        [!Border.IsVisibleProperty] = new Binding("UnreadCount") { Converter = new Avalonia.Data.Converters.FuncValueConverter<int, bool>(count => count > 0) },
                                                        Child = new TextBlock()
                                                        {
                                                            [!TextBlock.TextProperty] = new Binding("UnreadCount"),
                                                            Foreground = Brushes.White,
                                                            FontSize = 11,
                                                            FontWeight = FontWeight.Bold,
                                                            HorizontalAlignment = HorizontalAlignment.Center,
                                                            VerticalAlignment = VerticalAlignment.Center,
                                                            Margin = Avalonia.Thickness.Parse("6,0")
                                                        }
                                                    }
                                                }
                                            }
                                        };
                                    })
                                },
                                new ListBox()
                                {
                                    [Grid.RowProperty] = 1,
                                    Background = Brush.Parse("#161616"),
                                    [!ListBox.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsSearchVisible)),
                                    [!ListBox.ItemsSourceProperty] = new Binding(nameof(MainWindowViewModel.SearchResult)),
                                    [!ListBox.SelectedItemProperty] = new Binding(nameof(MainWindowViewModel.SelectedUser)) { Mode = BindingMode.TwoWay },
                                    ItemTemplate = new FuncDataTemplate<User>((user, namescope) =>
                                    {
                                        return new Border()
                                        {
                                            BorderBrush = Brush.Parse("#2e2e2e"),
                                            BorderThickness = Avalonia.Thickness.Parse("2"),
                                            CornerRadius = Avalonia.CornerRadius.Parse("8"),
                                            Margin = Avalonia.Thickness.Parse("1"),
                                            Background = Brush.Parse("#161616"),
                                            Child = new StackPanel()
                                            {
                                                Orientation = Orientation.Horizontal,
                                                Spacing = 10,
                                                Margin = Avalonia.Thickness.Parse("0,6"),
                                                Children =
                                                {
                                                    new Border()
                                                    {
                                                        Width = 40,
                                                        Height = 40,
                                                        CornerRadius = new Avalonia.CornerRadius(20),
                                                        Background = Brush.Parse("#424141"),
                                                        VerticalAlignment = VerticalAlignment.Center
                                                    },
                                                    new TextBlock()
                                                    {
                                                        [!TextBlock.TextProperty] = new Binding("Username"),
                                                        Foreground = Brushes.White,
                                                        FontWeight = FontWeight.Medium,
                                                        VerticalAlignment = VerticalAlignment.Center
                                                    }
                                                }
                                            }
                                        };
                                    })
                                }
                            }
                        },
                        new Grid()
                        {
                            [Grid.ColumnProperty] = 1,
                            RowDefinitions = new RowDefinitions("40, *, 60"),
                            Background = Brush.Parse("#121212"),
                            Children =
                            {
                                new Border()
                                {
                                    [Grid.RowProperty] = 0,
                                    Background = Brush.Parse("#1A1A1A"),
                                    Child = new TextBlock()
                                    {
                                        [!TextBlock.TextProperty] = new Binding(nameof(MainWindowViewModel.StatusMessage)),
                                        Foreground = Brushes.White,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Margin = Avalonia.Thickness.Parse("10,0")
                                    }
                                },
                                _chat,
                                new Grid()
                                {
                                    [Grid.RowProperty] = 2,
                                    ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto"),
                                    Background = Brush.Parse("#1E1E1E"),
                                    Children =
                                    {
                                        new TextBox()
                                        {
                                            AcceptsReturn = true,
                                            [Grid.ColumnProperty] = 1,
                                            Watermark = "Написать сообщение...",
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Margin = Avalonia.Thickness.Parse("10"),
                                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.NewMessageText)) { Mode = BindingMode.TwoWay }
                                        },
                                        new Button()
                                        {
                                            [Grid.ColumnProperty] = 2,
                                            Content = "Отправить",
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Margin = Avalonia.Thickness.Parse("0,0,10,0"),
                                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.OnSendClicked)),
                                            HotKey = new KeyGesture(Key.Enter),
                                        },
                                        new Button()
                                        {
                                            [Grid.ColumnProperty] = 0,
                                            [!Button.IsEnabledProperty] = new Binding(nameof(MainWindowViewModel.IsUploading))
                                            {
                                                Converter = new FuncValueConverter<bool, bool>(isUp => !isUp)
                                            },
                                            [!Button.ContentProperty] = new Binding(nameof(MainWindowViewModel.AttachButtonText)),
                                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.OnAttachClick))
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = Avalonia.Thickness.Parse("15"),
                    Spacing = 8,
                    Children =
                    {
                        new Border()
                        {
                            Width = 10,
                            Height = 10,
                            CornerRadius = new Avalonia.CornerRadius(5),
                            VerticalAlignment = VerticalAlignment.Center,
                            [!Border.BackgroundProperty] = new Binding(nameof(MainWindowViewModel.IndicatorColor)),
                        },
                        new TextBlock
                        {
                            [!TextBlock.TextProperty] = new Binding(nameof(MainWindowViewModel.IndicatorText)),
                            Foreground = Brushes.White,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 12
                        }
                    }
                }
            }
        };
    }

    public class MessageConvertor : IValueConverter
    {
        private readonly MainWindowViewModel _vm;

        public MessageConvertor(MainWindowViewModel vm)
        {
            _vm = vm;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Guid senderid)
            {
                if (senderid == _vm.MyId)
                {
                    return HorizontalAlignment.Right;
                }
                else
                {
                    return HorizontalAlignment.Left;
                }
            }

            return BindingNotification.UnsetValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BubbleColorConverter : IValueConverter
    {
        private readonly MainWindowViewModel _vm;

        public BubbleColorConverter(MainWindowViewModel vm)
        {
            _vm = vm;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Guid senderId)
            {
                if (senderId == _vm.MyId)
                {
                    return Brush.Parse("#16395c");
                }
                return Brush.Parse("#202020");
            }
            return Brush.Parse("#202020");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime time)
            {
                return shTools.FormatTime(time);
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MessageStatus status)
            {
                if (status == MessageStatus.Sent) return "✓";
                if (status == MessageStatus.Delivered) return "✓✓";
                if (status == MessageStatus.Read) return "✓✓";
            }
            return "";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}