﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomizeBuildings
{
    public class Helper
    {
        /// <summary>Clamps the value between Min and Max.</summary>
        /// <param name="Min">the minimum the value must be</param>
        /// <param name="Value">value to be clamped</param>
        /// <param name="Max">the maximum the value must be</param>
        /// <returns></returns>
        public static int MinMax(int Min, int Value, int Max)
        {
            return Math.Max(Math.Min(Max, Value), Min);
        }

        /// <summary>Clamps the value between Min and Max.</summary>
        /// <param name="Min">the minimum the value must be</param>
        /// <param name="Value">value to be clamped</param>
        /// <param name="Max">the maximum the value must be</param>
        /// <returns></returns>
        public static float MinMax(float Min, float Value, float Max)
        {
            return Math.Max(Math.Min(Max, Value), Min);
        }
    }
}