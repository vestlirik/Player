using System;
using System.Windows;
using System.Windows.Controls;
using Un4seen.Bass;

namespace VkAudioWpf
{
    /// <summary>
    /// Interaction logic for Equalizer.xaml
    /// </summary>
    public partial class Equalizer : Window
    {
        private Settings _sett;
        public Equalizer(Settings sett)
        {
            InitializeComponent();
            _sett = sett;
        }
        
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int index=int.Parse(((Slider)sender).Name.Split('z')[1]);
            _sett.ChangeEqValue(index, (float)((Slider)sender).Value);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            float[] values = _sett.GetEQValues();
            sliderz0.Value = values[0];
            sliderz1.Value = values[1];
            sliderz2.Value = values[2];
            sliderz3.Value = values[3];
            sliderz4.Value = values[4];
            sliderz5.Value = values[5];
            sliderz6.Value = values[6];
            sliderz7.Value = values[7];
            sliderz8.Value = values[8];
            sliderz9.Value = values[9];
            sliderz10.Value = values[10];
            sliderz11.Value = values[11];

            sliderz0.ValueChanged += Slider_ValueChanged;
            sliderz1.ValueChanged += Slider_ValueChanged;
            sliderz2.ValueChanged += Slider_ValueChanged;
            sliderz3.ValueChanged += Slider_ValueChanged;
            sliderz4.ValueChanged += Slider_ValueChanged;
            sliderz5.ValueChanged += Slider_ValueChanged;
            sliderz6.ValueChanged += Slider_ValueChanged;
            sliderz7.ValueChanged += Slider_ValueChanged;
            sliderz8.ValueChanged += Slider_ValueChanged;
            sliderz9.ValueChanged += Slider_ValueChanged;
            sliderz10.ValueChanged += Slider_ValueChanged;
            sliderz11.ValueChanged += Slider_ValueChanged;
        }
    }
}
