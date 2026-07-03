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
using LocalMimu.Models;
using System.Globalization;
using Avalonia.Data.Converters;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MimuGui.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;

    public MainWindow()
    {
        _vm = new MainWindowViewModel();
        DataContext = _vm;

        BuildUI();
    }

    private void BuildUI()
    {
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

                        new TextBlock()
                        {
                            Text = "Регистрация",
                            HorizontalAlignment = HorizontalAlignment.Center
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
    [!Grid.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsLoginVisible)) {Converter = Avalonia.Data.Converters.BoolConverters.Not},
    Children =
    {
        // ==========================================
        // левая колонка
        // ==========================================
        new Grid()
        {
            [Grid.ColumnProperty] = 0,
            RowDefinitions = new RowDefinitions("Auto, *"),
            Background = Brush.Parse("#141414"),
            Children =
            {
                // СТРОКА 0: поиск
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

                // СТРОКА 1: список
                new ListBox()
                {
                    [Grid.RowProperty] = 1,
                    Background = Brush.Parse("#141414"),
                    Styles =
                    {
                        new Style(x => x.OfType<ListBoxItem>().Class(":pointerover"))
                        {
                            Setters = { new Setter(ListBoxItem.BackgroundProperty, Brush.Parse("#272727")) }
                        },
                        new Style(x => x.OfType<ListBoxItem>().Class(":selected"))
                        {
                            Setters = { new Setter(ListBoxItem.BackgroundProperty, Brush.Parse("#272727")) }
                        }
                    },
                    [!ListBox.ItemsSourceProperty] = new Binding(nameof(MainWindowViewModel.ActiveChats)),
                    [!ListBox.SelectedItemProperty] = new Binding(nameof(MainWindowViewModel.SelectedUser)) { Mode = BindingMode.TwoWay },

                    ItemTemplate = new FuncDataTemplate<User>((user, namescope) =>
                    {
                        // левая колонка - чаты
                        return new Border()
                        {
                            CornerRadius = Avalonia.CornerRadius.Parse("6"),
                            Background = Brushes.Transparent,
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
                                    new StackPanel()
                                    {
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Children=
                                        {
                                            new TextBlock()
                                            {
                                            [!TextBlock.TextProperty] = new Binding("Username"),
                                            Foreground = Brushes.White,
                                            FontWeight = FontWeight.Medium,
                                            VerticalAlignment = VerticalAlignment.Center
                                            },
                                            new TextBlock()
                                            {
                                                Text = "Последнее сообщение",
                                                Foreground = Brush.Parse("#69686869"),
                                                FontSize = 12
                                            }
                                        
                                        }

                                    }
                                }
                            }
                        };
                    })
                },
                // поиск
                new ListBox()
                {
                    [Grid.RowProperty] = 1,
                    Background= Brush.Parse("#161616"),
                    [!ListBox.IsVisibleProperty] = new Binding(nameof(MainWindowViewModel.IsSearchVisible)),
                    [!ListBox.ItemsSourceProperty] = new Binding(nameof(MainWindowViewModel.SearchResult)),
                    [!ListBox.SelectedItemProperty] = new Binding(nameof(MainWindowViewModel.SelectedUser)) {Mode = BindingMode.TwoWay},

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

                new ListBox()
                {
                    [Grid.RowProperty] = 1,
                    Background = Brushes.Transparent,
                    [!ListBox.ItemsSourceProperty] = new Binding(nameof(MainWindowViewModel.ChatMessages)),

                    ItemTemplate = new FuncDataTemplate<Message>((msg, namescope) =>
                    {
                        return new Border()
                        {
                            [!Border.BackgroundProperty] = new Binding("SenderID") {Converter = new BubbleColorConverter(_vm)},
                            CornerRadius = new Avalonia.CornerRadius(10),
                            Margin = Avalonia.Thickness.Parse("5"),
                            Padding = Avalonia.Thickness.Parse("10"),
                            [!Border.HorizontalAlignmentProperty] = new Binding("SenderID") {Converter = new MessageConvertor(_vm) },
                            Child = new TextBlock()
                            {
                                [!TextBlock.TextProperty] = new Binding("Text"),
                                Foreground = Brushes.White,
                                TextWrapping = TextWrapping.Wrap
                            }
                        };
                    })
                },

                new Grid()
                {
                    [Grid.RowProperty] = 2,
                    ColumnDefinitions = new ColumnDefinitions("*, Auto"),
                    Background = Brush.Parse("#1E1E1E"),
                    Children =
                    {
                        new TextBox()
                        {
                            [Grid.ColumnProperty] = 0,
                            Watermark = "Написать сообщение...",
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = Avalonia.Thickness.Parse("10"),
                            [!TextBox.TextProperty] = new Binding(nameof(MainWindowViewModel.NewMessageText)) { Mode = BindingMode.TwoWay }
                        },
                        new Button()
                        {
                            [Grid.ColumnProperty] = 1,
                            Content = "Отправить",
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = Avalonia.Thickness.Parse("0,0,10,0"),
                            [!Button.CommandProperty] = new Binding(nameof(MainWindowViewModel.OnSendClicked))
                        }
                    }
                }
            }
        }
    }
}
            }
        };
    }
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









