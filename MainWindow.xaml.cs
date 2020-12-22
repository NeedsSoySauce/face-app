using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace face_project
{
    public class MainWindow : Window
    {

        private Button _submitButton;
        private TextBlock _textBlock;
        private TextBlock _consoleTextBlock;
        private Image _image;
        private Canvas _faceCanvas;
        private TextBox _subscriptionKeyTextBox;
        private TextBox _endpointTextBox;


        private List<Border> _borderPool;

        private string _path;

        private OpenFileDialog _dialog = new OpenFileDialog();

        public MainWindow()
        {
            InitializeComponent();

            _dialog.Filters.Add(new FileDialogFilter()
            {
                Name = "Image",
                Extensions = new List<string>() {
                    "jpg", "jpeg", "png", "gif", "bmp"
                }
            });
            _dialog.Title = "Select an image";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _submitButton = this.FindControl<Button>("submit");
            _textBlock = this.Find<TextBlock>("textBlock");
            _consoleTextBlock = this.Find<TextBlock>("consoleTextBlock");
            _image = this.Find<Image>("image");
            _faceCanvas = this.Find<Canvas>("faceCanvas");
            _subscriptionKeyTextBox = this.Find<TextBox>("subscriptionKeyTextBox");
            _endpointTextBox = this.Find<TextBox>("endpointTextBox");


            _borderPool = Enumerable.Range(0, 50).Select(s => new Border()
            {
                Margin = new Thickness(10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(118, 255, 3)),
                BorderThickness = new Thickness(1),
                IsVisible = false
            }).ToList();
            _faceCanvas.Children.AddRange(_borderPool);
        }

        IBitmap LoadImage(string path)
        {
            return new Bitmap(path);
        }

        public async void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var result = await _dialog.ShowAsync(this);

            if (result.Length == 1)
            {
                ClearFaceBorders();
                _consoleTextBlock.Text = null;
                _path = result[0];
                _textBlock.Text = _path;
                _image.MaxHeight = 400;
                _image.Source = LoadImage(_path);
            }
        }

        double Scale(double sourceLength, double destLenth, int value)
        {
            return value * destLenth / sourceLength;
        }

        void ClearFaceBorders()
        {
            _borderPool.ForEach(b => b.IsVisible = false);
        }

        public async void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (_path == null) return;

            Stream image = new FileStream(_path, FileMode.Open);

            var client = FaceService.Authenticate(_endpointTextBox.Text, _subscriptionKeyTextBox.Text);
            var result = await FaceService.DetectFacesFromStream(client, image);
            _consoleTextBlock.Text = result.Response;

            ClearFaceBorders();
            for (int i = 0; i < result.Faces.Count; i++)
            {
                var face = result.Faces[i];
                var border = _borderPool[i];

                var rect = face.FaceRectangle;

                border.Width = Scale(_image.Source.Size.Width, _image.Width, rect.Width);
                border.Height = Scale(_image.Source.Size.Height, _image.Height, rect.Height);

                var scaleTop = Scale(_image.Source.Size.Height, _image.Height, rect.Top);
                var scaleLeft = Scale(_image.Source.Size.Width, _image.Width, rect.Left);

                Canvas.SetTop(border, scaleTop);
                Canvas.SetLeft(border, scaleLeft);

                border.IsVisible = true;
            };
        }
    }
}