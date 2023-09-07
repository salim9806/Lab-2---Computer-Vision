using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Extensions.Configuration;

using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Drawing;
using Image = System.Drawing.Image;
using System.IO;
using System.Drawing.Imaging;

namespace Lab2_imageAiProcessing
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ComputerVisionClient cvClient;
		private string selectedImageFilePath;
		public MainWindow()
		{
			InitializeComponent();
			detectBtn.IsEnabled = false;

			// Get config settings from AppSettings
			IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
			IConfigurationRoot configuration = builder.Build();
			string cogSvcEndpoint = configuration["CognitiveServicesEndpoint"];
			string cogSvcKey = configuration["CognitiveServiceKey"];

			// Authenticate Computer Vision client
			ApiKeyServiceClientCredentials creds = new ApiKeyServiceClientCredentials(cogSvcKey);

			cvClient = new ComputerVisionClient(creds)
			{
				Endpoint = cogSvcEndpoint
			};
		}

		private void uploadImageBtn_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			//dlg.InitialDirectory = "c:\\";
			dlg.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
			dlg.RestoreDirectory = true;

			if ((bool)dlg.ShowDialog())
			{
				string selectedFileName = dlg.FileName;
				//FileNameLabel.Content = selectedFileName;
				string selectedImageFilePath = selectedFileName;
				BitmapImage bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.UriSource = new Uri(selectedFileName);
				bitmap.EndInit();

				resultImage.Source = null;
				uploadedImage.Source = bitmap;

				detectBtn.IsEnabled = true;
				resultText.Text = "";
			}
		}

		private async void detectBtn_Click(object sender, RoutedEventArgs e)
		{
			List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>() {
				VisualFeatureTypes.Objects
			};

			string filePath = "";

			if (uploadedImage.Source is BitmapImage bitmapImage)
			{
				Uri uri = bitmapImage.UriSource;

				if (uri != null && uri.IsFile)
				{
					filePath = uri.LocalPath;
				}
			}


			Image image = Image.FromFile(filePath);
			using (var imageData = File.OpenRead(filePath))
			{

				var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);
				if(analysis.Objects.Count > 0)
				{
					Graphics graphics = Graphics.FromImage(image);
					System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Cyan, 3);
					Font font = new Font("Arial", 60);
					SolidBrush brush = new SolidBrush(System.Drawing.Color.White);

					foreach (var detectedObject in analysis.Objects)
					{
						// Draw object bounding box
						var r = detectedObject.Rectangle;
						System.Drawing.Rectangle rect = new System.Drawing.Rectangle(r.X, r.Y, r.W, r.H);
						graphics.DrawRectangle(pen, rect);
						graphics.DrawString(detectedObject.ObjectProperty, font, brush, r.X, r.Y);

					}

					resultText.Text = $"{analysis.Objects.Count} objects detected.";

					resultImage.Source = ConvertImageToBitmapImage(image);
				}else
				{
					resultText.Text = "Could not detect any object.";
				}
			}

		}
		private BitmapImage ConvertImageToBitmapImage(Image image)
		{
			using (var ms = new MemoryStream())
			{
				image.Save(ms, ImageFormat.Bmp);
				ms.Seek(0, SeekOrigin.Begin);

				var bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.StreamSource = ms;
				bitmapImage.EndInit();

				return bitmapImage;
			}
		} 
	}
}
