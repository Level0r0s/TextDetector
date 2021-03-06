﻿using DigitalVideoProcessingLib.VideoFrameType;
using DigitalVideoProcessingLib.VideoType;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalVideoProcessingLib.Interface
{
    public interface IVideoSaver
    {
        Task<bool> SaveVideoAsync(GreyVideo video, System.Drawing.Pen pen, string pathToSave, string framesSubDir, string framesExpansion);
        Task<bool> SaveVideoAsync(List<Bitmap> bitmapFrames, string pathToSave, string framesSubDir, string framesExpansion);
        Task<bool> SaveVideoFrameAsync(GreyVideoFrame videoFrame, System.Drawing.Pen pen, string saveFileName);
    }
}
