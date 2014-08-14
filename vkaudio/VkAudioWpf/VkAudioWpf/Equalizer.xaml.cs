using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Un4seen.Bass;
using WPFSoundVisualizationLib;

namespace VkAudioWpf
{
    /// <summary>
    /// Interaction logic for Equalizer.xaml
    /// </summary>
    public partial class Equalizer : Window
    {
        private int _stream;
        Timer timer; 
        public Equalizer(int streamHandle)
        {
            InitializeComponent();
            _stream = streamHandle;
            Call_Equalizer();
            timer = new Timer();
            timer.Interval = 800;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            float[] values = equalizer.EqualizerValues;
            for (int i = 0; i < 7; i++)
            {
                UpdateEQ(i, values[i]);
            }
        }

        int[] _fxEQ = new int[7];
        #region Call_Equalizer

        private void Call_Equalizer()
        {
            float[] values = equalizer.EqualizerValues;
            // 7-band EQ
            BASS_DX8_PARAMEQ eq = new BASS_DX8_PARAMEQ();

            _fxEQ[0] = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
            _fxEQ[1] = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
            _fxEQ[2] = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
            _fxEQ[3] = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
            _fxEQ[4] = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
            _fxEQ[5] = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
            _fxEQ[6] = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);

            eq.fBandwidth = 18f;

            // EQ1
            eq.fCenter = 80f;  // max. Tiefe
            eq.fGain = 0;
            Bass.BASS_FXSetParameters(_fxEQ[0], eq);
            eq.fGain = values[0];
            Bass.BASS_FXSetParameters(_fxEQ[0], eq);
            // EQ2
            eq.fCenter = 250f;
            eq.fGain = 0;
            Bass.BASS_FXSetParameters(_fxEQ[1], eq);
            eq.fGain = values[1];
            Bass.BASS_FXSetParameters(_fxEQ[1], eq);
            // EQ3
            eq.fCenter = 500f;
            eq.fGain = 0;
            Bass.BASS_FXSetParameters(_fxEQ[2], eq);
            eq.fGain = values[2];
            Bass.BASS_FXSetParameters(_fxEQ[2], eq);
            // EQ4
            eq.fCenter = 1000f;
            eq.fGain = values[3];
            Bass.BASS_FXSetParameters(_fxEQ[3], eq);
            eq.fGain = 0;
            Bass.BASS_FXSetParameters(_fxEQ[3], eq);
            // EQ5
            eq.fCenter = 6000f;
            eq.fGain = 0;
            Bass.BASS_FXSetParameters(_fxEQ[4], eq);
            eq.fGain = values[4];
            Bass.BASS_FXSetParameters(_fxEQ[4], eq);
            // EQ6
            eq.fCenter = 12000f;
            eq.fGain = 0;
            Bass.BASS_FXSetParameters(_fxEQ[5], eq);
            eq.fGain = values[5];
            Bass.BASS_FXSetParameters(_fxEQ[5], eq);
            // EQ7
            eq.fCenter = 16000f;
            eq.fGain = 0;
            Bass.BASS_FXSetParameters(_fxEQ[6], eq);
            eq.fGain = values[6];
            Bass.BASS_FXSetParameters(_fxEQ[6], eq);
        }


        private void UpdateEQ(int band, float gain)
        {
            BASS_DX8_PARAMEQ eq = new BASS_DX8_PARAMEQ();
            if (Bass.BASS_FXGetParameters(_fxEQ[band], eq))
            {
                eq.fGain = gain;
                Bass.BASS_FXSetParameters(_fxEQ[band], eq);
            }
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
