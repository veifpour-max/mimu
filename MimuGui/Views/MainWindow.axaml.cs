using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using MimuGui.ViewModels;
using MimuGui.Design;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Media;
using Avalonia.Controls.Templates;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using Avalonia.Input;
using LocalMimu.Models;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;


namespace MimuGui.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _vm = viewModel;
        _vm.StorageService = new WindowStorageService(this);
        DataContext = _vm;
        BuildUI();
        _vm.ChatMessages.CollectionChanged += (s, e) =>
        {
            if (_vm.ChatItems.Count == 0) return;

            var lastItem = _vm.ChatItems[^1];

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _chat.ScrollIntoView(lastItem);
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

    private Control BuildPlusButton()
    {
        var btn = new Button()
        {
            Width = 48,
            Height = 48,
            Background = Palette.Accent,
            CornerRadius = Avalonia.CornerRadius.Parse("24"),
            Content = new Path
            {
                Data = Icons.Add,
                Width = 26,
                Height = 26,
                Stretch = Stretch.Uniform,
                Fill = Palette.OnSurface
            },
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = Avalonia.Thickness.Parse("0,0,15,72"),
            [!Button.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsChatOpen))
            {
                Converter = new Avalonia.Data.Converters.FuncValueConverter<bool, bool>(open => !open)
            }
        };

        btn.Click += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm) vm.ToggleGroupMenu();
        };

        return btn;
    }

    private Control BuildGroupCreationMenu()
    {
        var closeButton = new Button()
        {
            Width = 30,
            Height = 30,
            Background = Brushes.Transparent,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = new Path
            {
                Data = Icons.Close,
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform,
                Fill = Palette.OnSurface
            },
            HorizontalAlignment = HorizontalAlignment.Right,
            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.ToggleGroupMenu))
        };

        var content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new Grid()
                {
                    ColumnDefinitions = new ColumnDefinitions("*, Auto"),
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = "Новая группа",
                            FontSize = 16,
                            FontWeight = FontWeight.Bold,
                            Foreground = Palette.OnSurface,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        closeButton
                    }
                },
                new TextBox
                {
                    Width = 300,
                    Watermark = "Как вы назовете свою группу?",
                    Background = Palette.SurfaceContainerLowest,
                    Foreground = Palette.OnSurface,
                    [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.NewGroupName)) { Mode = BindingMode.TwoWay }
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Children =
                    {
                        new TextBox
                        {
                            Width = 220,
                            Watermark = "Username добавляемого",
                            Background = Palette.SurfaceContainerLowest,
                            Foreground = Palette.OnSurface,
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.DraftSearchText)) { Mode = BindingMode.TwoWay }
                        },
                        new Button
                        {
                            Content = "Добавить",
                            Background = Palette.SurfaceContainerHigh,
                            Foreground = Palette.OnSurface,
                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.AddUserToDraftAsync))
                        }
                    }
                },
                new ListBox
                {
                    Width = 300,
                    Height = 150,
                    Background = Brushes.Transparent,
                    [!ListBox.ItemsSourceProperty] = new Binding(nameof(MainWindowViewModel.DraftedMembers)),
                    ItemTemplate = new FuncDataTemplate<User>((user, namescope) =>
                        new Border
                        {
                            Padding = Avalonia.Thickness.Parse("5"),
                            Background = Palette.SurfaceContainerHighest,
                            CornerRadius = new Avalonia.CornerRadius(5),
                            Child = new TextBlock
                            {
                                Foreground = Palette.OnSurface,
                                [!TextBlock.TextProperty] = new Binding("Username")
                            }
                        }
                    )
                },
                new Button
                {
                    Width = 300,
                    Content = "Создать группу",
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Background = Palette.OnSurface,
                    Foreground = Palette.Background,
                    [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.CreateGroupCommand))
                }
            }
        };

        return new Border
        {
            [!Border.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsGroupMenuVisible)),
            Background = Palette.SurfaceContainer,
            BorderBrush = Palette.OutlineVariant,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(12),
            Padding = new Avalonia.Thickness(16),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = content
        };
    }

    private void BuildUI()
    {
        _chat = new ListBox()
        {
            [Grid.RowProperty] = 1,
            MaxWidth = 760,
            Background = Brushes.Transparent,
            [!ListBox.ItemsSourceProperty] = new Binding(nameof(MainWindowViewModel.ChatItems)),
        };
        _chat.DataTemplates.Add(new FuncDataTemplate<DaySeparator>((sep, namescope) =>
            new Border
            {
                BorderBrush = Palette.OutlineVariant,
                BorderThickness = new Avalonia.Thickness(0, 0, 0, 1),
                Margin = Avalonia.Thickness.Parse("5,6,5,2"),
                Child = new TextBlock
                {
                    Text = sep.Label,
                    FontSize = 11,
                    Foreground = Palette.OnSurfaceVariant,
                    Margin = Avalonia.Thickness.Parse("0,0,0,6")
                }
            }));
        _chat.DataTemplates.Add(new FuncDataTemplate<Message>((msg, namescope) =>
            {
                var downloadBtn = new Button()
                {
                    Content = "Скачать",
                    Background = Palette.SurfaceContainerLowest,
                    Foreground = Palette.OnSurface,
                    CornerRadius = new Avalonia.CornerRadius(5),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                var downloadProgress = new ProgressBar()
                {
                    IsVisible = false,
                    Minimum = 0,
                    Maximum = 100,
                    Height = 4,
                    Width = 140,
                    Foreground = Palette.OnSurface,
                    Background = Palette.SurfaceContainerHigh,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                var openFolderBtn = new Button()
                {
                    Content = "Открыть папку",
                    IsVisible = false,
                    Background = Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0),
                    Foreground = Palette.OnSurface,
                    Padding = Avalonia.Thickness.Parse("2"),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                var fileBlock = new StackPanel()
                {
                    Spacing = 4,
                    Margin = Avalonia.Thickness.Parse("0, 5, 0, 0"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    [!StackPanel.IsVisibleProperty] = new Binding("Type")
                    {
                        Converter = new Avalonia.Data.Converters.FuncValueConverter<MessageType, bool>(type => type == MessageType.File)
                    },
                    Children = { downloadBtn, downloadProgress, openFolderBtn }
                };

                string? downloadedPath = null;
                downloadBtn.Click += (sender, e) =>
                {
                    downloadBtn.IsVisible = false;
                    downloadProgress.IsVisible = true;
                    downloadProgress.Value = 0;
                    _ = _vm.DownloadFileAsync(msg,
                        p => Avalonia.Threading.Dispatcher.UIThread.Post(() => downloadProgress.Value = p * 100),
                        path => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            downloadProgress.IsVisible = false;
                            if (path != null)
                            {
                                downloadedPath = path;
                                openFolderBtn.IsVisible = true;
                            }
                            else
                            {
                                downloadBtn.IsVisible = true;
                            }
                        }));
                };
                openFolderBtn.Click += (sender, e) =>
                {
                    if (downloadedPath == null) return;
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{downloadedPath}\"") { UseShellExecute = true });
                };

                return new Border()
                {
                    [!Border.BackgroundProperty] = new Binding("SenderID") { Converter = new BubbleColorConverter(_vm) },
                    [!Border.CornerRadiusProperty] = new Binding("SenderID") { Converter = new BubbleRadiusConvertor(_vm) },
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
                        [!TextBlock.TextProperty] = new Binding("SenderUsername"),
                        [!TextBlock.IsVisibleProperty] = new Binding("SenderID") { Converter = new AuthorVisibilityConverter(_vm) },
                        FontSize = 10,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Palette.OnSurfaceVariant
                    },
                    new TextBlock()
                    {
                        [!TextBlock.TextProperty] = new Binding("DisplayText"),
                        Foreground = Palette.OnSurface,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock()
                    {
                        [!TextBlock.TextProperty] = new Binding("SentAt") {Converter = new TimeConverter()},
                        FontSize = 10,
                        Foreground = Palette.OnSurfaceVariant,
                        HorizontalAlignment = HorizontalAlignment.Right,
                    },
                    new Path()
                    {
                        [!Path.DataProperty] = new Binding("Status") { Converter = new StatusIconConverter() },
                        [!Path.IsVisibleProperty] = new Binding("SenderID")
{
    Converter = new Avalonia.Data.Converters.FuncValueConverter<Guid, bool>(id => id == _vm.MyId)
},
                        Width = 12,
                        Height = 12,
                        Stretch = Stretch.Uniform,
                        Fill = Palette.OnSurfaceVariant,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    fileBlock
                        }
                    }
                };
            }));


        this.Width = 800;
        this.Height = 600;
        this.Title = "LocalMimu";

        var sidebarList = new ListBox()
        {
            [Grid.RowProperty] = 1,
            Background = Palette.SurfaceContainerLow,
            Styles =
            {
                new Style(x => x.OfType<ListBoxItem>().Class(":pointerover"))
                { Setters = { new Setter(ListBoxItem.BackgroundProperty, Palette.SurfaceContainerHigh) } },
                new Style(x => x.OfType<ListBoxItem>().Class(":selected"))
                { Setters = { new Setter(ListBoxItem.BackgroundProperty, Palette.SurfaceContainerHigh), new Setter(ListBoxItem.BorderThicknessProperty, new Avalonia.Thickness(3, 0, 0, 0)), new Setter(ListBoxItem.BorderBrushProperty, Palette.Accent) } }
            },
            [!ListBox.ItemsSourceProperty] = new Binding(nameof(MainWindowViewModel.SidebarItems)),
            [!ListBox.SelectedItemProperty] = new Binding(nameof(MainWindowViewModel.SelectedChat)) { Mode = BindingMode.TwoWay },
        };
        sidebarList.DataTemplates.Add(new FuncDataTemplate<GroupChat>((group, namescope) =>
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
                            Background = Palette.SurfaceContainerHighest,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = Avalonia.Thickness.Parse("0,0,10,0"),
                            Child = new Path
                            {
                                Data = Icons.Group,
                                Width = 22,
                                Height = 22,
                                Stretch = Stretch.Uniform,
                                Fill = Palette.OnSurfaceVariant,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        },
                        new StackPanel()
                        {
                            [Grid.ColumnProperty] = 1,
                            VerticalAlignment = VerticalAlignment.Center,
                            Children =
                            {
                                new StackPanel()
                                {
                                    Orientation = Orientation.Horizontal,
                                    Spacing = 6,
                                    Children =
                                    {
                                        new TextBlock()
                                        {
                                            [!TextBlock.TextProperty] = new Binding("Name"),
                                            Foreground = Palette.OnSurface,
                                            FontWeight = FontWeight.Bold,
                                            FontSize = 14,
                                            TextTrimming = TextTrimming.CharacterEllipsis
                                        },
                                        new Border()
                                        {
                                            CornerRadius = new Avalonia.CornerRadius(3),
                                            BorderBrush = Palette.OutlineVariant,
                                            BorderThickness = new Avalonia.Thickness(1),
                                            Padding = new Avalonia.Thickness(5, 1),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Child = new TextBlock
                                            {
                                                Text = "группа",
                                                FontSize = 10,
                                                Foreground = Palette.OnSurfaceVariant
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }));
        sidebarList.DataTemplates.Add(new FuncDataTemplate<User>((user, namescope) =>
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
                            Background = Palette.SurfaceContainerHighest,
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
                                    Foreground = Palette.OnSurface,
                                    FontWeight = FontWeight.Bold,
                                    FontSize = 14
                                },
                                new StackPanel()
                                {
                                    Orientation = Orientation.Horizontal,
                                    Spacing = 4,
                                    Children =
                                    {
                                        new TextBlock()
                                        {
                                            [!TextBlock.TextProperty] = new Binding("LastMessageText"),
                                            Foreground = Palette.OnSurfaceVariant,
                                            FontSize = 12,
                                            MaxLines = 1,
                                            TextTrimming = TextTrimming.CharacterEllipsis
                                        },
                                        new Path()
                                        {
                                            [!Path.DataProperty] = new MultiBinding
                                            {
                                                Bindings = { new Binding("Id"), new Binding("LastMessageText") },
                                                Converter = new LastMessageTickConverter(_vm)
                                            },
                                            Width = 12,
                                            Height = 12,
                                            Stretch = Stretch.Uniform,
                                            Fill = Palette.OnSurfaceVariant,
                                            VerticalAlignment = VerticalAlignment.Center
                                        }
                                    }
                                }
                            }
                        },
                        new StackPanel()
                        {
                            [Grid.ColumnProperty] = 2,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Children =
                            {
                                new TextBlock()
                                {
                                    [!TextBlock.TextProperty] = new MultiBinding
                                    {
                                        Bindings = { new Binding("Id"), new Binding("LastMessageText") },
                                        Converter = new LastMessageTimeConverter(_vm)
                                    },
                                    Foreground = Palette.OnSurfaceVariant,
                                    FontSize = 10,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                },
                                new Border()
                                {
                                    Background = Palette.Accent,
                                    CornerRadius = new Avalonia.CornerRadius(10),
                                    MinWidth = 20,
                                    Height = 20,
                                    HorizontalAlignment = HorizontalAlignment.Right,
                                    Margin = Avalonia.Thickness.Parse("0,3,0,0"),
                                    [!Border.IsVisibleProperty] = new Binding("UnreadCount") { Converter = new Avalonia.Data.Converters.FuncValueConverter<int, bool>(count => count > 0) },
                                    Child = new TextBlock()
                                    {
                                        [!TextBlock.TextProperty] = new Binding("UnreadCount"),
                                        Foreground = Palette.OnSurface,
                                        FontSize = 11,
                                        FontWeight = FontWeight.Bold,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Margin = Avalonia.Thickness.Parse("6,0")
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }));

        this.Content = new Panel()
        {
            Background = Palette.Background,
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
                            Background = Palette.SurfaceContainerLowest,
                            Foreground = Palette.OnSurface,
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.Username)) { Mode = BindingMode.TwoWay }
                        },
                        new TextBox()
                        {
                            Width = 300,
                            Watermark = "Введите пароль",
                            Background = Palette.SurfaceContainerLowest,
                            Foreground = Palette.OnSurface,
                            PasswordChar='*',
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.Password)) { Mode = BindingMode.TwoWay }
                        },
                        new Button()
                        {
                            Width = 150,
                            Height = 35,
                            Background = Palette.OnSurface,
                            Foreground = Palette.Background,
                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.OnLogClicked)),
                            Content = "Войти",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            VerticalContentAlignment = VerticalAlignment.Center
                        },
                        new Button()
                        {
                            Background = Palette.SurfaceContainerHigh,
                            Foreground = Palette.OnSurface,
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
                            Foreground = Palette.Error,
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
                            Background = Palette.SurfaceContainerLowest,
                            Foreground = Palette.OnSurface,
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.RegName)) { Mode = BindingMode.TwoWay }
                        },
                        new TextBox()
                        {
                            Width = 300,
                            Watermark = "Введите username",
                            Background = Palette.SurfaceContainerLowest,
                            Foreground = Palette.OnSurface,
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.RegUsername)) { Mode = BindingMode.TwoWay }
                        },
                        new TextBox()
                        {
                            Width = 300,
                            Watermark = "Введите пароль",
                            Background = Palette.SurfaceContainerLowest,
                            Foreground = Palette.OnSurface,
                            PasswordChar='*',
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.RegPassword)) { Mode = BindingMode.TwoWay }
                        },
                        new Button()
                        {
                            Width = 150,
                            Height = 35,
                            Background = Palette.OnSurface,
                            Foreground = Palette.Background,
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
                            Background = Palette.SurfaceContainerHigh,
                            Foreground = Palette.OnSurface,
                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.SwitchToLogin))
                        },
                        new TextBlock()
                        {
                            [!TextBlock.TextProperty] = new Binding(nameof(MainWindowViewModel.StatusMessage)),
                            Foreground = Palette.Error,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 14
                        }
                    }
                },
                new Grid()
                {
                    ColumnDefinitions = new ColumnDefinitions("250,*"),
                    Background = Palette.Surface,
                    [!Grid.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsMainVisible)),
                    Children =
                    {
                        new Grid()
                        {
                            [Grid.ColumnProperty] = 0,
                            RowDefinitions = new RowDefinitions("Auto, *, Auto, Auto"),
                            Background = Palette.SurfaceContainerLow,
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
                                            Background = Palette.SurfaceContainerHigh,
                                            BorderThickness = new Avalonia.Thickness(0),
                                            CornerRadius = new Avalonia.CornerRadius(9),
                                            Foreground = Palette.OnSurface,
                                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.SearchingText)) { Mode = BindingMode.TwoWay }
                                        },
                                        new Button()
                                        {
                                            [Grid.ColumnProperty] = 1,
                                            Width = 36,
                                            Height = 36,
                                            Margin = Avalonia.Thickness.Parse("5,0,0,0"),
                                            Padding = new Avalonia.Thickness(0),
                                            Background = Palette.Primary,
                                            CornerRadius = new Avalonia.CornerRadius(18),
                                            HorizontalContentAlignment = HorizontalAlignment.Center,
                                            VerticalContentAlignment = VerticalAlignment.Center,
                                            Content = new Path
                                            {
                                                Data = Icons.ArrowForward,
                                                Width = 20,
                                                Height = 20,
                                                Stretch = Stretch.Uniform,
                                                Fill = Palette.OnSurface
                                            },
                                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.SearchingUserAsync))
                                        },
                                        new Button()
                                        {
                                            [Grid.ColumnProperty] = 2,
                                            [!Button.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsSearchVisible)),
                                            Background = Brushes.Transparent,
                                            Content = new Path
                                            {
                                                Data = Icons.Close,
                                                Width = 12,
                                                Height = 12,
                                                Stretch = Stretch.Uniform,
                                                Fill = Palette.OnSurface
                                            },
                                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.CancelSearch)),
                                            Foreground = Palette.OnSurface,
                                            CornerRadius = Avalonia.CornerRadius.Parse("18"),
                                            Margin = Avalonia.Thickness.Parse("5,0,0,0")
                                        }
                                    }
                                },
                                sidebarList,
                                new ListBox()
                                {
                                    [Grid.RowProperty] = 1,
                                    Background = Palette.SurfaceContainer,
                                    [!ListBox.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsSearchVisible)),
                                    [!ListBox.ItemsSourceProperty] = new Binding(nameof(MainWindowViewModel.SearchResult)),
                                    [!ListBox.SelectedItemProperty] = new Binding(nameof(MainWindowViewModel.SelectedUser)) { Mode = BindingMode.TwoWay },
                                    ItemTemplate = new FuncDataTemplate<User>((user, namescope) =>
                                    {
                                        return new Border()
                                        {
                                            BorderBrush = Palette.OutlineVariant,
                                            BorderThickness = Avalonia.Thickness.Parse("2"),
                                            CornerRadius = Avalonia.CornerRadius.Parse("8"),
                                            Margin = Avalonia.Thickness.Parse("1"),
                                            Background = Palette.SurfaceContainer,
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
                                                        Background = Palette.SurfaceContainerHighest,
                                                        VerticalAlignment = VerticalAlignment.Center
                                                    },
                                                    new TextBlock()
                                                    {
                                                        [!TextBlock.TextProperty] = new Binding("Username"),
                                                        Foreground = Palette.OnSurface,
                                                        FontWeight = FontWeight.Medium,
                                                        VerticalAlignment = VerticalAlignment.Center
                                                    }
                                                }
                                            }
                                        };
                                    })
                                },
                                new Border()
                                {
                                    [Grid.RowProperty] = 2,
                                    BorderBrush = Palette.OutlineVariant,
                                    BorderThickness = new Avalonia.Thickness(0, 1, 0, 0),
                                    Child = new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Margin = Avalonia.Thickness.Parse("12,7"),
                                        Spacing = 7,
                                        Children =
                                        {
                                            new Border()
                                            {
                                                Width = 8,
                                                Height = 8,
                                                CornerRadius = new Avalonia.CornerRadius(4),
                                                VerticalAlignment = VerticalAlignment.Center,
                                                [!Border.BackgroundProperty] = new Binding(nameof(MainWindowViewModel.IndicatorColor))
                                            },
                                            new TextBlock()
                                            {
                                                [!TextBlock.TextProperty] = new Binding(nameof(MainWindowViewModel.IndicatorText)),
                                                Foreground = Palette.OnSurfaceVariant,
                                                FontSize = 11,
                                                VerticalAlignment = VerticalAlignment.Center
                                            },
                                            new Button()
                                            {
                                                Width = 26,
                                                Height = 26,
                                                Margin = Avalonia.Thickness.Parse("0,0,0,0"),
                                                HorizontalAlignment = HorizontalAlignment.Right,
                                                HorizontalContentAlignment = HorizontalAlignment.Center,
                                                VerticalContentAlignment = VerticalAlignment.Center,
                                                Padding = new Avalonia.Thickness(0),
                                                Background = Brushes.Transparent,
                                                BorderThickness = new Avalonia.Thickness(1),
                                                BorderBrush = Palette.OutlineVariant,
                                                CornerRadius = new Avalonia.CornerRadius(13),
                                                [ToolTip.TipProperty] = "Переподключиться",
                                                [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.ReconnectNow)),
                                                Content = new Path
                                                {
                                                    Data = Icons.ArrowsClockwise,
                                                    Width = 13,
                                                    Height = 13,
                                                    Stretch = Stretch.Uniform,
                                                    Fill = Palette.OnSurfaceVariant
                                                }
                                            }
                                        }
                                    }
                                },
                                new Border()
                                {
                                    [Grid.RowProperty] = 3,
                                    BorderBrush = Palette.OutlineVariant,
                                    BorderThickness = new Avalonia.Thickness(0, 1, 0, 0),
                                    Child = new Grid()
                                    {
                                        ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto"),
                                        Margin = Avalonia.Thickness.Parse("12,8"),
                                        Children =
                                        {
                                            new Grid()
                                            {
                                                [Grid.ColumnProperty] = 0,
                                                Width = 34,
                                                Height = 34,
                                                Children =
                                                {
                                                    new Border()
                                                    {
                                                        Width = 34,
                                                        Height = 34,
                                                        CornerRadius = new Avalonia.CornerRadius(17),
                                                        Background = Palette.SurfaceContainerHighest,
                                                        Child = new TextBlock
                                                        {
                                                            FontSize = 12,
                                                            FontWeight = FontWeight.SemiBold,
                                                            Foreground = Palette.OnSurfaceVariant,
                                                            HorizontalAlignment = HorizontalAlignment.Center,
                                                            VerticalAlignment = VerticalAlignment.Center
                                                        }
                                                    },
                                                    new Border()
                                                    {
                                                        Width = 10,
                                                        Height = 10,
                                                        CornerRadius = new Avalonia.CornerRadius(5),
                                                        Background = Palette.Online,
                                                        BorderBrush = Palette.SurfaceContainerLow,
                                                        BorderThickness = new Avalonia.Thickness(2),
                                                        HorizontalAlignment = HorizontalAlignment.Right,
                                                        VerticalAlignment = VerticalAlignment.Bottom,
                                                        Margin = Avalonia.Thickness.Parse("0,0,-1,-1")
                                                    }
                                                }
                                            },
                                            new StackPanel()
                                            {
                                                [Grid.ColumnProperty] = 1,
                                                VerticalAlignment = VerticalAlignment.Center,
                                                Margin = Avalonia.Thickness.Parse("10,0,0,0"),
                                                Children =
                                                {
                                                    new TextBlock()
                                                    {
                                                        [!TextBlock.TextProperty] = new Binding(nameof(MainWindowViewModel.MyUsername)),
                                                        Foreground = Palette.OnSurface,
                                                        FontSize = 13,
                                                        FontWeight = FontWeight.SemiBold
                                                    },
                                                    new TextBlock()
                                                    {
                                                        Text = "в сети",
                                                        Foreground = Palette.OnSurfaceVariant,
                                                        FontSize = 11
                                                    }
                                                }
                                            },
                                            new Button()
                                            {
                                                [Grid.ColumnProperty] = 2,
                                                Width = 32,
                                                Height = 32,
                                                Background = Brushes.Transparent,
                                                Padding = new Avalonia.Thickness(0),
                                                HorizontalContentAlignment = HorizontalAlignment.Center,
                                                VerticalContentAlignment = VerticalAlignment.Center,
                                                [ToolTip.TipProperty] = "Настройки",
                                                Content = new Path
                                                {
                                                    Data = Icons.Gear,
                                                    Width = 18,
                                                    Height = 18,
                                                    Stretch = Stretch.Uniform,
                                                    Fill = Palette.OnSurfaceVariant
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },


                        new Grid()
                        {
                            [Grid.ColumnProperty] = 1,
                            RowDefinitions = new RowDefinitions("52, *, 60"),
                            Background = Palette.Surface,
                            Children =
                            {
                                new Border()
                                {
                                    [Grid.RowProperty] = 0,
                                    Background = Palette.SurfaceContainer,
                                    Child = new StackPanel()
                                    {
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Margin = Avalonia.Thickness.Parse("10,0"),
                                        Children =
                                        {
                                            new StackPanel()
                                            {
                                                Orientation = Orientation.Horizontal,
                                                Spacing = 8,
                                                Children =
                                                {
                                                    new TextBlock()
                                                    {
                                                        [!TextBlock.TextProperty] = new Binding(nameof(MainWindowViewModel.CurrentChatTitle)),
                                                        Foreground = Palette.OnSurface,
                                                        FontWeight = FontWeight.Bold,
                                                        FontSize = 13,
                                                        TextTrimming = TextTrimming.CharacterEllipsis
                                                    },
                                                    new Border()
                                                    {
                                                        CornerRadius = new Avalonia.CornerRadius(3),
                                                        BorderBrush = Palette.OutlineVariant,
                                                        BorderThickness = new Avalonia.Thickness(1),
                                                        Padding = new Avalonia.Thickness(5, 1),
                                                        VerticalAlignment = VerticalAlignment.Center,
                                                        [!Border.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsGroupSelected)),
                                                        Child = new TextBlock
                                                        {
                                                            Text = "группа",
                                                            FontSize = 10,
                                                            Foreground = Palette.OnSurfaceVariant
                                                        }
                                                    }
                                                }
                                            },
                                            new TextBlock()
                                            {
                                                [!TextBlock.TextProperty] = new Binding(nameof(MainWindowViewModel.StatusMessage)),
                                                Foreground = Palette.OnSurfaceVariant,
                                                FontSize = 11,
                                                TextTrimming = TextTrimming.CharacterEllipsis
                                            }
                                        }
                                    }
                                },
                                _chat,
                                new Grid()
                                {
                                    [Grid.RowProperty] = 2,
                                    ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto"),
                                    Background = Palette.SurfaceContainer,
                                    Children =
                                    {
                                        new TextBox()
                                        {
                                            AcceptsReturn = true,
                                            [Grid.ColumnProperty] = 1,
                                            Watermark = "Написать сообщение...",
                                            Background = Palette.SurfaceContainerHigh,
                                            BorderThickness = new Avalonia.Thickness(0),
                                            Foreground = Palette.OnSurface,
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Margin = Avalonia.Thickness.Parse("10"),
                                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.NewMessageText)) { Mode = BindingMode.TwoWay }
                                        },
                                        new Button()
                                        {
                                            [Grid.ColumnProperty] = 2,
                                            Width = 40,
                                            Height = 40,
                                            Background = Palette.Primary,
                                            CornerRadius = new Avalonia.CornerRadius(20),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Margin = Avalonia.Thickness.Parse("0,0,10,0"),
                                            HorizontalContentAlignment = HorizontalAlignment.Center,
                                            VerticalContentAlignment = VerticalAlignment.Center,
                                            Content = new Path
                                            {
                                                Data = Icons.Send,
                                                Width = 20,
                                                Height = 20,
                                                Stretch = Stretch.Uniform,
                                                Fill = Palette.OnSurface
                                            },
                                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.OnSendClicked)),
                                            HotKey = new KeyGesture(Key.Enter),
                                        },
                                        new Button()
                                        {
                                            [Grid.ColumnProperty] = 0,
                                            Margin = Avalonia.Thickness.Parse("6,0,0,0"),
                                            Background = Palette.SurfaceContainerHigh,
                                            Foreground = Palette.OnSurface,
                                            [!Button.IsEnabledProperty] = new Binding(nameof(MainWindowViewModel.IsUploading))
                                            {
                                                Converter = new FuncValueConverter<bool, bool>(isUp => !isUp)
                                            },
                                            Content = new Path
                                            {
                                                Data = Icons.Attach,
                                                Width = 18,
                                                Height = 18,
                                                Stretch = Stretch.Uniform,
                                                Fill = Palette.OnSurface,
                                                HorizontalAlignment = HorizontalAlignment.Center,
                                                VerticalAlignment = VerticalAlignment.Center
                                            },
                                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.OnAttachClick))
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                BuildPlusButton(),
                BuildGroupCreationMenu()
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
                    return Palette.Primary;
                }
                return Palette.SurfaceContainer;
            }
            return Palette.SurfaceContainer;
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

    public class BubbleRadiusConvertor : IValueConverter
    {
        private readonly MainWindowViewModel _vm;

        public BubbleRadiusConvertor(MainWindowViewModel vm)
        {
            _vm = vm;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Guid id && id == _vm.MyId)
            {
                return new Avalonia.CornerRadius(10, 10, 3, 10);
            }
            return new Avalonia.CornerRadius(10, 10, 10, 3);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class AuthorVisibilityConverter : IValueConverter
    {
        private readonly MainWindowViewModel _vm;

        public AuthorVisibilityConverter(MainWindowViewModel vm)
        {
            _vm = vm;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return _vm.SelectedGroup != null && value is Guid id && id != _vm.MyId;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class LastMessageTimeConverter : IMultiValueConverter
    {
        private readonly MainWindowViewModel _vm;

        public LastMessageTimeConverter(MainWindowViewModel vm)
        {
            _vm = vm;
        }

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count > 0 && values[0] is Guid id && _vm.GetLastMessageInfo(id) is { } info)
            {
                return info.Time.Date == DateTime.Now.Date ? shTools.FormatTime(info.Time) : info.Time.ToString("dd.MM");
            }
            return "";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class LastMessageTickConverter : IMultiValueConverter
    {
        private readonly MainWindowViewModel _vm;

        public LastMessageTickConverter(MainWindowViewModel vm)
        {
            _vm = vm;
        }

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count > 0 && values[0] is Guid id && _vm.GetLastMessageInfo(id) is { } info && info.IsOwn)
            {
                return info.Status == MessageStatus.Delivered || info.Status == MessageStatus.Read ? Icons.DoneAll : Icons.Check;
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MessageStatus status)
            {
                if (status == MessageStatus.Sent) return Icons.Check;
                if (status == MessageStatus.Delivered) return Icons.DoneAll;
                if (status == MessageStatus.Read) return Icons.DoneAll;
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}