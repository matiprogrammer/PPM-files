using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;

namespace Grafika_PPM_Files
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            TransformGroup group = new TransformGroup();

            ScaleTransform xform = new ScaleTransform();
            group.Children.Add(xform);

            TranslateTransform tt = new TranslateTransform();
            group.Children.Add(tt);

            imgPhoto.RenderTransform = group;
            this.MouseWheel += image_MouseWheel;
        }

        private Bitmap bitmap;
        int maxValue;
        private void LoadButtonClick(object sender, RoutedEventArgs e)
        {
            int width, height;
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "PPM Files (*.ppm) |*.ppm";

            if (op.ShowDialog() == true)
            {
                string result;
                string[] lines;
                using (StreamReader streamReader = new StreamReader(op.FileName))
                {
                    result = streamReader.ReadToEnd();

                }

                lines = result.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                lines = lines.Where(line => line[0] != '#').ToArray();
                string[] values = String.Join(" ", lines).Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (result.Substring(0, 2) == "P3")
                {

                    width = Int32.Parse(values[1]);
                    height = Int32.Parse(values[2]);
                    maxValue = Int32.Parse(values[3]);
                    if (values.Length < width * height * 3 + 4)
                        MessageBox.Show("Wymiary bloku danych nie są poprawne! \nZadeklarowana: " + width * height * 3 + "\nRzeczywista: " + (values.Length - 4));
                    else
                    {
                        bitmap = CreateBitmap(width, height, values);
                        imgPhoto.Source = ImageSourceForBitmap(bitmap);
                    }

                }
                else if (result.Substring(0, 2) == "P6")
                {

                    var reader = new BinaryReader(new FileStream(op.FileName, FileMode.Open));
                    if (reader.ReadChar() != 'P' || reader.ReadChar() != '6') { }

                    while ((Convert.ToChar(reader.PeekChar())) == ' ' || (Convert.ToChar(reader.PeekChar())) == '\n')
                        reader.ReadChar();
                    string widths = "", heights = "", max = "";
                    char temp;
                    if ((temp = Convert.ToChar(reader.PeekChar())) == '#')
                        while ((temp = reader.ReadChar()) != '\n') ;
                    while ((temp = reader.ReadChar()) != ' ' && temp != '\n')
                    {
                        widths += temp;
                    }
                    if ((temp = Convert.ToChar(reader.PeekChar())) == '#')
                        while ((temp = reader.ReadChar()) != '\n') ;
                    while ((Convert.ToChar(reader.PeekChar())) == ' ' || (Convert.ToChar(reader.PeekChar())) == '\n')
                        reader.ReadChar();
                    while ((temp = reader.ReadChar()) >= '0' && temp <= '9')
                    {
                        heights += temp;
                    }
                    while ((Convert.ToChar(reader.PeekChar())) == ' ' || (Convert.ToChar(reader.PeekChar())) == '\n')
                        reader.ReadChar();
                    while ((temp = reader.ReadChar()) != ' ' && temp != '\n')
                        max += temp;

                    width = Int32.Parse(widths);
                    height = Int32.Parse(heights);
                    maxValue = Int32.Parse(max);
                    bitmap = CreateBitmap(width, height, ReadAllBytes(reader));
                    imgPhoto.Source = ImageSourceForBitmap(bitmap);

                }
                else
                {
                    MessageBoxResult messageBox = MessageBox.Show("Nieprawidłowy format: " + result.Substring(0, 2) + "!");
                }
            }
        }

        public byte[] ReadAllBytes(BinaryReader reader)
        {
            const int bufferSize = 4096;
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[bufferSize];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                return ms.ToArray();
            }

        }


        public Bitmap CreateBitmap(int width, int height, string[] values)
        {
            Bitmap bitmap = new Bitmap(width, height);
            string[] pixelValues = new string[width * 3];
            int x = 0;
            for (int j = 0; j < height; j++)
            {
                //pixelValues = ReadData(values, width*3);
                for (int i = 0; i < width; i++)
                {

                    bitmap.SetPixel(i, j, System.Drawing.Color.FromArgb(Int32.Parse(values[x + 4]) * 255 / maxValue, Int32.Parse(values[x + 5]) * 255 / maxValue, Int32.Parse(values[x + 6]) * 255 / maxValue));
                    x += 3;
                }
            }
            return bitmap;
        }
        public Bitmap CreateBitmap(int width, int height, byte[] bytes)
        {
            if (bytes.Length < width * height * 3)
            {
                MessageBox.Show("Wymiary bloku danych nie są poprawne! \nZadeklarowana: " + width * height * 3 + "\nRzeczywista: " + (bytes.Length - 4));
                return new Bitmap(width, height);
            }
            Bitmap bitmap = new Bitmap(width, height);
            int x = 0;
            for (int j = 0; j < height; j++)
            {

                for (int i = 0; i < width; i++)
                {
                    bitmap.SetPixel(i, j, System.Drawing.Color.FromArgb((int)bytes[x] * 255 / maxValue, (int)bytes[x + 1] * 255 / maxValue, (int)bytes[x + 2] * 255 / maxValue));
                    x += 3;
                }

            }
            return bitmap;
        }

        public ImageSource ImageSourceForBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());


        }

        private void SaveJPEGClick(object sender, RoutedEventArgs e)
        {
            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);


            System.Drawing.Imaging.Encoder myEncoder =
                System.Drawing.Imaging.Encoder.Quality;

            EncoderParameters myEncoderParameters = new EncoderParameters(1);

            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder,Int32.Parse(QualityTextBox.Text));
            myEncoderParameters.Param[0] = myEncoderParameter;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter =
                "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg";

            if (saveFileDialog.ShowDialog() == true)
            {
                bitmap.Save(saveFileDialog.FileName, jpgEncoder, myEncoderParameters);
            }

        }

        private void LoadJPEGFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg";

            if (op.ShowDialog() == true)
            {
                bitmap = new Bitmap(op.FileName);
                imgPhoto.Source = ImageSourceForBitmap(bitmap);
            }
        }

        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TransformGroup transformGroup = (TransformGroup)imgPhoto.RenderTransform;
            ScaleTransform transform = (ScaleTransform)transformGroup.Children[0];

            double zoom = e.Delta > 0 ? .2 : -.2;
            transform.ScaleX += zoom;
            transform.ScaleY += zoom;
            Console.WriteLine(e.GetPosition(imgPhoto).X / imgPhoto.ActualWidth + " " + e.GetPosition(imgPhoto).Y / imgPhoto.ActualHeight);
            imgPhoto.RenderTransformOrigin = new System.Windows.Point(e.GetPosition(imgPhoto).X / imgPhoto.ActualWidth, e.GetPosition(imgPhoto).Y / imgPhoto.ActualHeight);
        }
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}

