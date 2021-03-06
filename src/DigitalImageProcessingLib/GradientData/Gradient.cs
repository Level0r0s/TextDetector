﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalImageProcessingLib.GradientData
{
    public enum RoundGradientDirection {DEGREE_0 = 0, DEGREE__45 = 45, DEGREE_90 = 90, DEGREE_135 = 135, UNDEFINED }
    public class Gradient
    {
        public static double UNDEFINED_VALUE = -1.0;
        public int Strength { get; set; }
        public int Angle { get; set; }
        public RoundGradientDirection RoundGradientDirection { get; set; }
        public double GradientX { get; set; }
        public double GradientY { get; set; }
        public double StepX { get; set; }
        public double StepY { get; set; }
        public double Magnitude { get; set; }
    }
}
