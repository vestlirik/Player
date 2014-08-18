using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkAudioWpf
{
    class EqualizerSettings
    {

        float[] _hzEQ;
        public float[] _vlEQ;
        public int[] _fxEQ=new int[12];

        public EqualizerSettings()
        {
            _hzEQ = new float[12];
            _vlEQ = new float[12];
            SetFrequncies();
            SetValues();
        }

        public float GetFraquencyByIndex(int index)
        {
            return _hzEQ[index];
        }
        public float GetValueByIndex(int index)
        {
            return _vlEQ[index];
        }

        private void SetFrequncies()
        {
            _hzEQ[0] = 80;
            _hzEQ[1] = 120;
            _hzEQ[2] = 230;
            _hzEQ[3] = 350;
            _hzEQ[4] = 700;
            _hzEQ[5] = 1600;
            _hzEQ[6] = 3200;
            _hzEQ[7] = 4600;
            _hzEQ[8] = 7000;
            _hzEQ[9] = 10000;
            _hzEQ[10] = 12000;
            _hzEQ[11] = 16000;
        }

        private void SetValues()
        {
            for (int i=0; i<12;i++)
            {
                _vlEQ[i] = 0;
            }
        }

        public void ChangeValue(int band,float value)
        {
            if (_hzEQ.Length > band && band >= 0)
                    _vlEQ[band] = value;
        }

        internal float[] GetValues()
        {
            return _vlEQ;
        }
    }
}
