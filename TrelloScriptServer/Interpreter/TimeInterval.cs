using System;
using System.Collections.Generic;
using System.Text;

namespace TrelloScriptServer.Interpreter
{
    struct TimeInterval
    {
        private float h;
        public float hours
        {
            get
            {
                return h;
            }
            set
            {
                if (value % 1.0f > 0)
                {
                    m += value % 1.0f * 60.0f;
                    value = (float)Math.Floor(value);
                }
                h = value;
            }
        }
        private float m;
        public float minutes
        {
            get
            {
                return m;
            }
            set
            {
                m = value;
                if (m >= 60)
                {
                    h++;
                    m %= 60;
                }
            }
        }

        public TimeInterval(float Hours, float Minutes)
        {
            h = Hours;
            m = Minutes;
        }

        public static TimeInterval operator *(TimeInterval timeInterval, float multi)
        {
            float minutes = timeInterval.h * 60 + timeInterval.m;
            minutes *= multi;
            float newValue = (float)MathF.Round(minutes);
            TimeInterval ret = new TimeInterval((float)Math.Floor(newValue / 60), newValue % 60);
            return ret;
        }
    }
}
