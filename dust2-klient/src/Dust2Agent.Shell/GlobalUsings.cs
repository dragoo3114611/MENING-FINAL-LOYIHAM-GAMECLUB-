// WPF va WinForms bir loyihada bo'lgani uchun bir nechta nom to'qnashadi (CS0104).
// Taxalluslar butun loyiha bo'ylab WPF turini tanlaydi; WinForms turlari App.xaml.cs'da
// `WinForms` prefiksi bilan ishlatiladi.
global using Application = System.Windows.Application;
global using Color = System.Windows.Media.Color;
global using KeyEventArgs = System.Windows.Input.KeyEventArgs;
