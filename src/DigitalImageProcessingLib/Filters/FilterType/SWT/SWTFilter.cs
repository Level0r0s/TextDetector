﻿using DigitalImageProcessingLib.ColorType;
using DigitalImageProcessingLib.ImageType;
using DigitalImageProcessingLib.SWTData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalImageProcessingLib.Filters.FilterType.SWT
{
    public class SWTFilter: Filter
    {
        private GreyImage _minIntensityDirectionImage = null;
        private GreyImage _maxIntensityDirectionImage = null;
        private GreyImage _gaussSmoothedImage = null;

        private List<Ray> _rayMinIntensityDirection = null;
        private List<Ray> _rayMaxIntensityDirection = null;

        public delegate bool CompareIntensity(int firstIntensity, int secondIntensity);
        public SWTFilter(GreyImage gaussSmoothedImage) 
        {
            if (gaussSmoothedImage == null)
                throw new ArgumentNullException("Null gaussSmoothedImage in ctor");
            this._gaussSmoothedImage = gaussSmoothedImage;
            this._rayMaxIntensityDirection = new List<Ray>();
            this._rayMinIntensityDirection = new List<Ray>();
        }

        public GreyImage MinIntensityDirectionImage() { return this._minIntensityDirectionImage; }
        public GreyImage MaxIntensityDirectionImage() { return this._maxIntensityDirectionImage; }

        /// <summary>
        /// Применение SWT фильтра к серому изображению
        /// </summary>
        /// <param name="image">Серое изображение</param>
        public override void Apply(GreyImage image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException("Null image in Apply");
                if (image.Height != this._gaussSmoothedImage.Height || image.Width != this._gaussSmoothedImage.Width)
                    throw new ArgumentException("Image must be the same size with gaussSmoothedImage in ctor");
                this._maxIntensityDirectionImage = (GreyImage) image.Copy();
                if (this._maxIntensityDirectionImage == null)
                    throw new NullReferenceException("Null _minIntensityDirectionImage in Apply");
                this._minIntensityDirectionImage = (GreyImage)image.Copy();
                if (this._minIntensityDirectionImage == null)
                    throw new NullReferenceException("Null _minIntensityDirectionImage in Apply");

                FillMinIntensityImage(image);              
                FillMaxIntensityImage(image);                              
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public override void Apply(RGBImage image)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Заполняет матрицу в направлении увеличения интенсивности
        /// </summary>
        /// <param name="image">Изображение</param>
        private void FillMaxIntensityImage(GreyImage image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException("Null image in FillMaxIntensityDirectionImage");
                FillStrokeImage(image, this._maxIntensityDirectionImage, this._rayMaxIntensityDirection, this.MinIntensity);
                TwoPassAlongRays(this._maxIntensityDirectionImage, this._rayMaxIntensityDirection);
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Заполняет матрицу в направлении уменьшения интенсивности
        /// </summary>
        /// <param name="image">Изображение</param>
        private void FillMinIntensityImage(GreyImage image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException("Null image in FillMinIntensityImage");
                FillStrokeImage(image, this._minIntensityDirectionImage, this._rayMinIntensityDirection, this.MaxIntensity);
                TwoPassAlongRays(this._minIntensityDirectionImage, this._rayMinIntensityDirection);
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Вычисляет карту SWT для изображения
        /// </summary>
        /// <param name="image">Изображение, представляющее границы изображения</param>
        /// <param name="fillingImage">Изображения для заполнения SWT карты</param>
        /// <param name="comparator">Функция сравнения интенсивностей пикселей</param>
        private void FillStrokeImage(GreyImage image, GreyImage fillingImage, List<Ray> rays, CompareIntensity comparator)
        {
            try
            {
                int imageHeight = image.Height - 1;
                int imageWidth = image.Width - 1;

                for (int i = 1; i < imageHeight; i++)
                    for (int j = 1; j < imageWidth; j++)
                    {
                        if (image.Pixels[i, j].BorderType == BorderType.Border.STRONG && !fillingImage.Pixels[i, j].StrokeWidth.WasProcessed)
                        {
                            int intensityI = 0;
                            int intensityJ = 0;
                            GetNeighboringPixel(comparator, i, j, ref intensityI, ref intensityJ);

                            if (intensityI == i && intensityJ == j + 1)
                                TrackRayRight(image, fillingImage, rays, comparator, i, j);
                            else if (intensityI == i && intensityJ == j - 1)
                                TrackRayLeft(image, fillingImage, rays, comparator, i, j);
                            else if (intensityJ == j && intensityI == i + 1)
                                TrackRayDown(image, fillingImage, rays, comparator, i, j);
                            else if (intensityJ == j && intensityI == i - 1)
                                TrackRayUp(image, fillingImage, rays, comparator, i, j);
                            else if (intensityI == i - 1 && intensityJ == j + 1)
                                TrackRayRightUp(image, fillingImage, rays, comparator, i, j);
                            else if (intensityI == i + 1 && intensityJ == j + 1)
                                TrackRayRightDown(image, fillingImage, rays, comparator, i, j);
                            else if (intensityI == i - 1 && intensityJ == j - 1)
                                TrackRayLeftUp(image, fillingImage, rays, comparator, i, j);
                            else if (intensityI == i + 1 && intensityJ == j - 1)
                                TrackRayLeftDown(image, fillingImage, rays, comparator, i, j); ;                            
                        }
                    }
            }
            catch (Exception exception)
            {
                throw exception;
            }

        }

        /// <summary>
        /// Второй проход по амссиву лучей и усреднение значение ширины штриха пикселей
        /// </summary>
        /// <param name="swtImage">SWT - карта</param>
        /// <param name="rays">Лучи</param>
        private void TwoPassAlongRays(GreyImage swtImage, List<Ray> rays)
        {
            try
            {
                int raysNumber = rays.Count;
                for (int i = 0; i < raysNumber; i++)                                   
                    switch (rays[i].Direction)
                    {
                        case RayDirection.LEFT:
                        {
                            double mean = this.CountSWTMeanRowRay(swtImage, rays[i]);
                            this.AveragingRowRayPixels(swtImage, rays[i], mean);
                            break;
                        }
                        case RayDirection.RIGHT:
                        {
                            double mean = this.CountSWTMeanRowRay(swtImage, rays[i]);
                            this.AveragingRowRayPixels(swtImage, rays[i], mean);
                            break;
                        }
                        case RayDirection.UP:
                        {
                            double mean = this.CountSWTMeanColumnRay(swtImage, rays[i]);
                            this.AveragingColumnRayPixels(swtImage, rays[i], mean);
                            break;
                        }
                        case RayDirection.DOWN:
                        {
                            double mean = this.CountSWTMeanColumnRay(swtImage, rays[i]);
                            this.AveragingColumnRayPixels(swtImage, rays[i], mean);
                            break;
                        }
                        case RayDirection.LEFT_UP:
                        {
                            double mean = this.CountSWTMeanDiagonalRay(swtImage, rays[i]);
                            this.AveragingDiagonalRayPixels(swtImage, rays[i], mean);
                            break;
                        }
                        case RayDirection.LEFT_DOWN:
                        {
                            double mean = this.CountSWTMeanDiagonalRay(swtImage, rays[i]);
                            this.AveragingDiagonalRayPixels(swtImage, rays[i], mean);
                            break;
                        }
                        case RayDirection.RIGHT_UP:
                        {
                            double mean = this.CountSWTMeanDiagonalRay(swtImage, rays[i]);
                            this.AveragingDiagonalRayPixels(swtImage, rays[i], mean);
                            break;
                        }
                        case RayDirection.RIGHT_DOWN:
                        {
                            double mean = this.CountSWTMeanDiagonalRay(swtImage, rays[i]);
                            this.AveragingDiagonalRayPixels(swtImage, rays[i], mean);
                            break;
                        }
                        default:
                            break;
                    }                
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Сравнивает 2 значения интенсивности
        /// </summary>
        /// <param name="firstIntensity">Первое значение интенсивности</param>
        /// <param name="secondIntensity">Второе значение интенсивности</param>
        /// <returns>1 - firstIntensity > secondIntensity, 0 - иначе</returns>
        private bool MaxIntensity(int firstIntensity, int secondIntensity)
        {
            return firstIntensity > secondIntensity ? true : false;
        }

        /// <summary>
        /// Сравнивает 2 значения интенсивности
        /// </summary>
        /// <param name="firstIntensity">Первое значение интенсивности</param>
        /// <param name="secondIntensity">Второе значение интенсивности</param>
        /// <returns>1 - firstIntensity < secondIntensity, 0 - иначе</returns>
        private bool MinIntensity(int firstIntensity, int secondIntensity)
        {
            return firstIntensity < secondIntensity ? true : false;
        }

       
        /// <summary>
        /// Находит индексы пискеля - соседа с максимальной или миннимальной интенсивнотью в зависимости от comparator
        /// </summary>
        /// <param name="comparator">Функция сравнения интенсивностей</param>
        /// <param name="row">Номер строки текущего пикселя</param>
        /// <param name="column">Номер столбца текущего пикселя</param>
        /// <param name="intensityI">Номер строки искомого пикселя</param>
        /// <param name="intensityJ">Номер столбца искомого пикселя</param>
        private void GetNeighboringPixel(CompareIntensity comparator, int row, int column, ref int intensityI, ref int intensityJ)
        {
            int intensity = _gaussSmoothedImage.Pixels[row - 1, column - 1].Color.Data;
            intensityI = row - 1;
            intensityJ = column - 1;
            if (comparator(intensity, _gaussSmoothedImage.Pixels[row - 1, column].Color.Data))
            {
                intensity = _gaussSmoothedImage.Pixels[row - 1, column].Color.Data;
                intensityI = row - 1;
                intensityJ = column;
            }           
            if (comparator(intensity, _gaussSmoothedImage.Pixels[row - 1, column + 1].Color.Data))
            {
                intensity = _gaussSmoothedImage.Pixels[row - 1, column + 1].Color.Data;
                intensityI = row - 1;
                intensityJ = column + 1;
            }
            if (comparator(intensity, _gaussSmoothedImage.Pixels[row, column - 1].Color.Data))
            {
                intensity = _gaussSmoothedImage.Pixels[row, column - 1].Color.Data;
                intensityI = row;
                intensityJ = column - 1;
            }
            if (comparator(intensity, _gaussSmoothedImage.Pixels[row, column + 1].Color.Data))
            {
                intensity = _gaussSmoothedImage.Pixels[row, column + 1].Color.Data;
                intensityI = row;
                intensityJ = column + 1;
            }
            if (comparator(intensity, _gaussSmoothedImage.Pixels[row + 1, column - 1].Color.Data))
            {
                intensity = _gaussSmoothedImage.Pixels[row + 1, column - 1].Color.Data;
                intensityI = row + 1;
                intensityJ = column - 1;
            }
            if (comparator(intensity, _gaussSmoothedImage.Pixels[row + 1, column].Color.Data))
            {
                intensity = _gaussSmoothedImage.Pixels[row + 1, column].Color.Data;
                intensityI = row + 1;
                intensityJ = column;
            }
            if (comparator(intensity, _gaussSmoothedImage.Pixels[row + 1, column + 1].Color.Data))
            {
                intensity = _gaussSmoothedImage.Pixels[row + 1, column + 1].Color.Data;
                intensityI = row + 1;
                intensityJ = column + 1;
            }
        }

        /// <summary>
        /// Строит луч от пикселя влево 
        /// </summary>
        /// <param name="image">Изображение, представляющее границы</param>
        /// <param name="fillingImage">Изображение для карты SWT</param>
        /// <param name="comparator">Функция сравнение интенсивностей пикселей</param>
        /// <param name="row">Номер строки пикселя, от которого строится луч</param>
        /// <param name="column">Номер столбца пикселя, от которого строится луч</param>
        private void TrackRayLeft(GreyImage image, GreyImage fillingImage, List<Ray> rays, CompareIntensity comparator, int row, int column)
        {
            try
            {
                int imageWidth = image.Width;
                int columnFrom = column;
                column -= 1;
                while (column >= 0 && image.Pixels[row, column].BorderType != BorderType.Border.STRONG)
                    --column;

                if (column >= 0)
                {
                    int intensityI = 0;
                    int intensityJ = 0;
                    GetNeighboringPixel(comparator, row, column, ref intensityI, ref intensityJ);
                    if (intensityI == row && intensityJ == column + 1)
                    {
                        fillingImage.Pixels[row, column].StrokeWidth.WasProcessed = true;
                        fillingImage.Pixels[row, columnFrom].StrokeWidth.WasProcessed = true;
                        FillRowWithStrokeWidth(fillingImage, column + 1, columnFrom, row, columnFrom - column - 1);                        
                    }
                    else
                    {
                        fillingImage.Pixels[row, columnFrom].StrokeWidth.WasProcessed = true;
                        FillRowWithStrokeWidth(fillingImage, column + 1, columnFrom, row, columnFrom - column - 1);
                    }

                    Ray ray = new Ray();
                    ray.ColumnBeginIndex = column + 1;
                    ray.ColumnEndIndex = columnFrom;
                    ray.RowBeginIndex = row;
                    ray.Direction = RayDirection.LEFT;

                    rays.Add(ray);
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Строит луч от пикселя вправо 
        /// </summary>
        /// <param name="image">Изображение, представляющее границы</param>
        /// <param name="fillingImage">Изображение для карты SWT</param>
        /// <param name="comparator">Функция сравнение интенсивностей пикселей</param>
        /// <param name="row">Номер строки пикселя, от которого строится луч</param>
        /// <param name="column">Номер столбца пикселя, от которого строится луч</param>
        private void TrackRayRight(GreyImage image, GreyImage fillingImage, List<Ray> rays, CompareIntensity comparator, int row, int column)
        {
            try
            {
                int imageWidth = image.Width;
                int columnFrom = column;
                column += 1;
                while (column < imageWidth && image.Pixels[row, column].BorderType != BorderType.Border.STRONG)
                    ++column;

                if (column < imageWidth)
                {
                    int intensityI = 0;
                    int intensityJ = 0;
                    GetNeighboringPixel(comparator, row, column, ref intensityI, ref intensityJ);

                    if (intensityI == row && intensityJ == column - 1)
                    {
                        fillingImage.Pixels[row, column].StrokeWidth.WasProcessed = true;
                        fillingImage.Pixels[row, columnFrom].StrokeWidth.WasProcessed = true;
                        FillRowWithStrokeWidth(fillingImage, columnFrom + 1, column, row, column - columnFrom - 1);
                    }
                    else
                    {
                        fillingImage.Pixels[row, columnFrom].StrokeWidth.WasProcessed = true;
                        FillRowWithStrokeWidth(fillingImage, columnFrom + 1, column, row, column - columnFrom - 1);
                    }
                    Ray ray = new Ray();
                    ray.ColumnBeginIndex = columnFrom + 1;
                    ray.ColumnEndIndex = column;
                    ray.RowBeginIndex = row;
                    ray.Direction = RayDirection.RIGHT;

                    rays.Add(ray);
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }

        }

        /// <summary>
        /// Строит луч от пикселя вверх 
        /// </summary>
        /// <param name="image">Изображение, представляющее границы</param>
        /// <param name="fillingImage">Изображение для карты SWT</param>
        /// <param name="comparator">Функция сравнение интенсивностей пикселей</param>
        /// <param name="row">Номер строки пикселя, от которого строится луч</param>
        /// <param name="column">Номер столбца пикселя, от которого строится луч</param>
        private void TrackRayUp(GreyImage image, GreyImage fillingImage, List<Ray> rays, CompareIntensity comparator, int row, int column)
        {
            try
            {
                int imageHeight = image.Height;
                int rowFrom = row;
                row -= 1;

              //  while (row >= 0 && image.Pixels[row, column].BorderType != BorderType.Border.STRONG)
               //     --row;

                for (; ;)
                {
                    if (row < 0 || image.Pixels[row, column].BorderType == BorderType.Border.STRONG ||
                        (image.Pixels[row, column + 1].BorderType == BorderType.Border.STRONG &&
                        image.Pixels[row, column - 1].BorderType == BorderType.Border.STRONG) ||

                        (image.Pixels[row, column + 1].BorderType == BorderType.Border.STRONG &&
                        image.Pixels[row - 1, column - 1].BorderType == BorderType.Border.STRONG) ||

                        (image.Pixels[row, column - 1].BorderType == BorderType.Border.STRONG &&
                        image.Pixels[row - 1, column + 1].BorderType == BorderType.Border.STRONG))
                        break;
                    --row;
                }

                if (row >= 0)
                {
                    int intensityI = 0;
                    int intensityJ = 0;
                    GetNeighboringPixel(comparator, row, column, ref intensityI, ref intensityJ);

                    if (column == intensityJ && intensityI == row + 1)
                    {
                        fillingImage.Pixels[row, column].StrokeWidth.WasProcessed = true;
                        fillingImage.Pixels[rowFrom, column].StrokeWidth.WasProcessed = true;
                        FillColumnWithStrokeWidth(fillingImage, row + 1, rowFrom, column, rowFrom - row - 1);
                    }
                    else
                    {
                        fillingImage.Pixels[rowFrom, column].StrokeWidth.WasProcessed = true;
                        FillColumnWithStrokeWidth(fillingImage, row + 1, rowFrom, column, rowFrom - row - 1);
                    }
                    Ray ray = new Ray();
                    ray.RowBeginIndex = row + 1;
                    ray.RowEndIndex = rowFrom;
                    ray.ColumnBeginIndex = column;
                    ray.Direction = RayDirection.UP;

                    rays.Add(ray);
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Строит луч от пикселя вниз 
        /// </summary>
        /// <param name="image">Изображение, представляющее границы</param>
        /// <param name="fillingImage">Изображение для карты SWT</param>
        /// <param name="comparator">Функция сравнение интенсивностей пикселей</param>
        /// <param name="row">Номер строки пикселя, от которого строится луч</param>
        /// <param name="column">Номер столбца пикселя, от которого строится луч</param>
        private void TrackRayDown(GreyImage image, GreyImage fillingImage, List<Ray> rays, CompareIntensity comparator, int row, int column)
        {
            try
            {
                int imageHeight = image.Height;
                int rowFrom = row;
                row += 1;

                while (row < imageHeight && image.Pixels[row, column].BorderType != BorderType.Border.STRONG)
                    ++row;

                if (row < imageHeight)
                {
                    int intensityI = 0;
                    int intensityJ = 0;
                    GetNeighboringPixel(comparator, row, column, ref intensityI, ref intensityJ);

                    if (column == intensityJ && intensityI == row - 1)
                    {
                        fillingImage.Pixels[row, column].StrokeWidth.WasProcessed = true;
                        fillingImage.Pixels[rowFrom, column].StrokeWidth.WasProcessed = true;
                        FillColumnWithStrokeWidth(fillingImage, rowFrom + 1, row, column, row - rowFrom - 1);
                    }
                    else
                    {
                        fillingImage.Pixels[rowFrom, column].StrokeWidth.WasProcessed = true;
                        FillColumnWithStrokeWidth(fillingImage, rowFrom + 1, row, column, row - rowFrom - 1);
                    }
                    Ray ray = new Ray();
                    ray.RowBeginIndex = rowFrom + 1;
                    ray.RowEndIndex = row;
                    ray.ColumnBeginIndex = column;
                    ray.Direction = RayDirection.DOWN;

                    rays.Add(ray);
                }

            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Строит луч от пикселя влево вверх
        /// </summary>
        /// <param name="image">Изображение, представляющее границы</param>
        /// <param name="fillingImage">Изображение для карты SWT</param>
        /// <param name="comparator">Функция сравнение интенсивностей пикселей</param>
        /// <param name="row">Номер строки пикселя, от которого строится луч</param>
        /// <param name="column">Номер столбца пикселя, от которого строится луч</param>
        private void TrackRayLeftUp(GreyImage image, GreyImage fillingImage, List<Ray> rays, CompareIntensity comparator, int row, int column)
        {
            try
            {
                int imageHeight = image.Height;
                int imageWidth = image.Width;
                int columnFrom = column;
                int rowfrom = row;

                column -= 1;
                row -= 1;

               /* while (row >= 0 && column >= 0 && image.Pixels[row, column].BorderType != BorderType.Border.STRONG)
                {
                    --row;
                    --column;
                }*/

                for (; ;)
                {
                    if (row - 1 < 0 || column - 1 < 0 || image.Pixels[row, column].BorderType == BorderType.Border.STRONG ||
                        (image.Pixels[row, column - 1].BorderType == BorderType.Border.STRONG &&
                        image.Pixels[row - 1, column].BorderType == BorderType.Border.STRONG))
                        break;
                    --row;
                    --column;
                }

                if (row - 1 >= 0 && column - 1 >= 0)
                {
                    int intensityI = 0;
                    int intensityJ = 0;
                    GetNeighboringPixel(comparator, row, column, ref intensityI, ref intensityJ);

                    if (row == intensityI - 1 && column == intensityJ - 1)
                    {
                        fillingImage.Pixels[row, column].StrokeWidth.WasProcessed = true;
                        fillingImage.Pixels[rowfrom, columnFrom].StrokeWidth.WasProcessed = true;
                        FillDiagonaWithStrokeWidth(fillingImage, row + 1, rowfrom, column + 1, 1, (int) ((rowfrom - row - 1) * Math.Sqrt(2.0)));
                    }
                    else
                    {
                        fillingImage.Pixels[rowfrom, columnFrom].StrokeWidth.WasProcessed = true;
                        FillDiagonaWithStrokeWidth(fillingImage, row + 1, rowfrom, column + 1, 1, (int) ((rowfrom - row - 1) * Math.Sqrt(2.0)));
                    }
                    Ray ray = new Ray();
                    ray.RowBeginIndex = row + 1;
                    ray.RowEndIndex = rowfrom;
                    ray.ColumnBeginIndex = column + 1;
                    ray.Direction = RayDirection.LEFT_UP;
                    ray.ColumnStep = 1;

                    rays.Add(ray);
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Строит луч от пикселя влево вниз
        /// </summary>
        /// <param name="image">Изображение, представляющее границы</param>
        /// <param name="fillingImage">Изображение для карты SWT</param>
        /// <param name="comparator">Функция сравнение интенсивностей пикселей</param>
        /// <param name="row">Номер строки пикселя, от которого строится луч</param>
        /// <param name="column">Номер столбца пикселя, от которого строится луч</param>
        private void TrackRayLeftDown(GreyImage image, GreyImage fillingImage, List<Ray> rays, CompareIntensity comparator, int row, int column)
        {
            try
            {
                int imageHeight = image.Height;
                int imageWidth = image.Width;
                int columnFrom = column;
                int rowfrom = row;

                column -= 1;
                row += 1;

             /*   while (row < imageHeight && column >= 0 && image.Pixels[row, column].BorderType != BorderType.Border.STRONG)
                {
                    ++row;
                    --column;
                }*/

                for (; ;)
                {
                    if (row + 1 >= imageHeight || column - 1 < 0 || image.Pixels[row, column].BorderType == BorderType.Border.STRONG ||
                        (image.Pixels[row, column - 1].BorderType == BorderType.Border.STRONG &&
                        image.Pixels[row + 1, column].BorderType == BorderType.Border.STRONG))
                        break;
                    ++row;
                    --column;
                }

                if (row + 1 < imageHeight && column - 1 >= 0)
                {
                    int intensityI = 0;
                    int intensityJ = 0;
                    GetNeighboringPixel(comparator, row, column, ref intensityI, ref intensityJ);

                    if (row == intensityI + 1 && column == intensityJ - 1)
                    {
                        fillingImage.Pixels[row, column].StrokeWidth.WasProcessed = true;
                        fillingImage.Pixels[rowfrom, columnFrom].StrokeWidth.WasProcessed = true;
                        FillDiagonaWithStrokeWidth(fillingImage, rowfrom + 1, row, columnFrom - 1, -1, (int) ((row - rowfrom - 1) * Math.Sqrt(2.0)));
                    }
                    else
                    {
                        fillingImage.Pixels[rowfrom, columnFrom].StrokeWidth.WasProcessed = true;
                        FillDiagonaWithStrokeWidth(fillingImage, rowfrom + 1, row, columnFrom - 1, -1, (int) ((row - rowfrom - 1) * Math.Sqrt(2.0)));
                    }
                    Ray ray = new Ray();
                    ray.RowBeginIndex = rowfrom + 1;
                    ray.RowEndIndex = row;
                    ray.ColumnBeginIndex = columnFrom - 1;
                    ray.Direction = RayDirection.LEFT_DOWN;
                    ray.ColumnStep = -1;

                    rays.Add(ray);
                }

            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Строит луч от пикселя вправо вверх 
        /// </summary>
        /// <param name="image">Изображение, представляющее границы</param>
        /// <param name="fillingImage">Изображение для карты SWT</param>
        /// <param name="comparator">Функция сравнение интенсивностей пикселей</param>
        /// <param name="row">Номер строки пикселя, от которого строится луч</param>
        /// <param name="column">Номер столбца пикселя, от которого строится луч</param>
        private void TrackRayRightUp(GreyImage image, GreyImage fillingImage, List<Ray> rays, CompareIntensity comparator, int row, int column)
        {
            try
            {
                int imageHeight = image.Height;
                int imageWidth = image.Width;
                int columnFrom = column;
                int rowfrom = row;

                column += 1;
                row -= 1;

                /*  while (row >= 0 && column < imageWidth && image.Pixels[row, column].BorderType != BorderType.Border.STRONG &&
                      (image.Pixels[row, column].BorderType != BorderType.Border.STRONG))
                  {
                      --row;
                      ++column;
                  }*/

                for (;;)
                {
                    if (row - 1 < 0 || column + 1 >= imageWidth || image.Pixels[row, column].BorderType == BorderType.Border.STRONG ||
                        (image.Pixels[row - 1, column].BorderType == BorderType.Border.STRONG && 
                        image.Pixels[row, column + 1].BorderType == BorderType.Border.STRONG))
                        break;
                    --row;
                    ++column;
                }

                if (column + 1 < imageWidth && row - 1 >= 0)
                {
                    int intensityI = 0;
                    int intensityJ = 0;
                    GetNeighboringPixel(comparator, row, column, ref intensityI, ref intensityJ);

                    if (row == intensityI - 1 && column == intensityJ + 1)
                    {
                        fillingImage.Pixels[row, column].StrokeWidth.WasProcessed = true;
                        fillingImage.Pixels[rowfrom, columnFrom].StrokeWidth.WasProcessed = true;
                        FillDiagonaWithStrokeWidth(fillingImage, row + 1, rowfrom, column - 1, -1, (int) ((rowfrom - row - 1) * Math.Sqrt(2.0)));
                    }
                    else
                    {
                        fillingImage.Pixels[rowfrom, columnFrom].StrokeWidth.WasProcessed = true;
                        FillDiagonaWithStrokeWidth(fillingImage, row + 1, rowfrom, column - 1, -1, (int) ((rowfrom - row - 1) * Math.Sqrt(2.0)));
                    }
                    Ray ray = new Ray();
                    ray.RowBeginIndex = row + 1;
                    ray.RowEndIndex = rowfrom;
                    ray.ColumnBeginIndex = column - 1;
                    ray.Direction = RayDirection.RIGHT_UP;
                    ray.ColumnStep = -1;

                    rays.Add(ray);
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }
        
        /// <summary>
        /// Строит луч от пикселя вправо вниз 
        /// </summary>
        /// <param name="image">Изображение, представляющее границы</param>
        /// <param name="fillingImage">Изображение для карты SWT</param>
        /// <param name="comparator">Функция сравнение интенсивностей пикселей</param>
        /// <param name="row">Номер строки пикселя, от которого строится луч</param>
        /// <param name="column">Номер столбца пикселя, от которого строится луч</param>
        private void TrackRayRightDown(GreyImage image, GreyImage fillingImage, List<Ray> rays, CompareIntensity comparator, int row, int column)
        {
            try
            {
                int imageHeight = image.Height;
                int imageWidth = image.Width;
                int columnFrom = column;
                int rowfrom = row;

                column += 1;
                row += 1;

                /*while (row < imageHeight && column < imageWidth && image.Pixels[row, column].BorderType != BorderType.Border.STRONG)
                {
                    ++row;
                    ++column;
                }*/
                for (; ;)
                {
                    if (row + 1 >= imageHeight || column + 1 >= imageWidth || image.Pixels[row, column].BorderType == BorderType.Border.STRONG ||
                        (image.Pixels[row, column + 1].BorderType == BorderType.Border.STRONG &&
                        image.Pixels[row + 1, column].BorderType == BorderType.Border.STRONG))
                        break;
                    ++row;
                    ++column;
                }

                if (row + 1 < imageHeight && column + 1 < imageWidth)
                {
                    int intensityI = 0;
                    int intensityJ = 0;
                    GetNeighboringPixel(comparator, row, column, ref intensityI, ref intensityJ);

                    if (row == intensityI + 1 && column == intensityJ + 1)
                    {
                        fillingImage.Pixels[row, column].StrokeWidth.WasProcessed = true;
                        fillingImage.Pixels[rowfrom, columnFrom].StrokeWidth.WasProcessed = true;
                        FillDiagonaWithStrokeWidth(fillingImage, rowfrom + 1, row, columnFrom + 1, 1, (int) ((row - rowfrom - 1) * Math.Sqrt(2.0)));
                    }
                    else
                    {
                        fillingImage.Pixels[rowfrom, columnFrom].StrokeWidth.WasProcessed = true;
                        FillDiagonaWithStrokeWidth(fillingImage, rowfrom + 1, row, columnFrom + 1, 1, (int) ((row - rowfrom - 1) * Math.Sqrt(2.0)));
                    }
                    Ray ray = new Ray();
                    ray.RowBeginIndex = rowfrom + 1;
                    ray.RowEndIndex = row;
                    ray.ColumnBeginIndex = columnFrom + 1;
                    ray.Direction = RayDirection.RIGHT_DOWN;
                    ray.ColumnStep = 1;

                    rays.Add(ray);
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Усреднение значений ширины штриха для пикселей горизонтального луча
        /// </summary>
        /// <param name="swtImage">SWT - карта</param>
        /// <param name="ray">Лучи</param>
        /// <param name="mean">Среднее значение ширины штриха</param>
        private void AveragingRowRayPixels(GreyImage swtImage, Ray ray, double mean)
        {
            try
            {
                int row = ray.RowBeginIndex;

                for (int i = ray.ColumnBeginIndex; i < ray.ColumnEndIndex; i++)
                {
                    if (swtImage.Pixels[row, i].StrokeWidth.Width > mean)
                        swtImage.Pixels[row, i].StrokeWidth.Width = (int) mean;
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Усреднение значений ширины штриха для пикселей вертикального луча
        /// </summary>
        /// <param name="swtImage">SWT - карта</param>
        /// <param name="ray">Лучи</param>
        /// <param name="mean">Среднее значение ширины штриха</param>
        private void AveragingColumnRayPixels(GreyImage swtImage, Ray ray, double mean)
        {
            try
            {
                int column = ray.ColumnBeginIndex;

                for (int i = ray.RowBeginIndex; i < ray.RowEndIndex; i++)
                {
                    if (swtImage.Pixels[i, column].StrokeWidth.Width > mean)
                        swtImage.Pixels[i, column].StrokeWidth.Width = (int) mean;
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Усреднение значений ширины штриха для пикселей диагонального луча
        /// </summary>
        /// <param name="swtImage">SWT - карта</param>
        /// <param name="ray">Лучи</param>
        /// <param name="mean">Среднее значение ширины штриха</param>
        private void AveragingDiagonalRayPixels(GreyImage swtImage, Ray ray, double mean)
        {
            try
            {
                for (int i = ray.RowBeginIndex, j = ray.ColumnBeginIndex; i < ray.RowEndIndex; i++, j += ray.ColumnStep)
                {
                    if (swtImage.Pixels[i, j].StrokeWidth.Width > mean)
                        swtImage.Pixels[i, j].StrokeWidth.Width = (int) mean;
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Вычисление среднего значения ширины штриха для горизонтального луча
        /// </summary>
        /// <param name="swtImage">SWT - Карта</param>
        /// <param name="ray">лучи</param>
        /// <returns>среднее</returns>
        private double CountSWTMeanRowRay(GreyImage swtImage, Ray ray)
        {
            try
            {
                double summ = 0.0;
                int pixelsNumberInRay = 0;
                int row = ray.RowBeginIndex;

                for (int i = ray.ColumnBeginIndex; i < ray.ColumnEndIndex; i++)
                {
                    summ += swtImage.Pixels[row, i].StrokeWidth.Width;
                    ++pixelsNumberInRay;
                }
                return (double) summ / (double) pixelsNumberInRay;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Вычисление среднего значения ширины штриха для вертикального луча
        /// </summary>
        /// <param name="swtImage">SWT - Карта</param>
        /// <param name="ray">лучи</param>
        /// <returns>среднее</returns>
        private double CountSWTMeanColumnRay(GreyImage swtImage, Ray ray)
        {
            try
            {
                double summ = 0.0;
                int pixelsNumberInRay = 0;
                int column = ray.ColumnBeginIndex;

                for (int i = ray.RowBeginIndex; i < ray.RowEndIndex; i++)
                {
                    summ += swtImage.Pixels[i, column].StrokeWidth.Width;
                    ++pixelsNumberInRay;
                }

                return (double)summ / (double)pixelsNumberInRay;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Вычисление среднего значения ширины штриха для диагонального луча
        /// </summary>
        /// <param name="swtImage">SWT - Карта</param>
        /// <param name="ray">лучи</param>
        /// <returns>среднее</returns>
        private double CountSWTMeanDiagonalRay(GreyImage swtImage, Ray ray)
        {
            try
            {
                double summ = 0.0;
                int pixelsNumberInRay = 0;

                for (int i = ray.RowBeginIndex, j = ray.ColumnBeginIndex; i < ray.RowEndIndex; i++, j += ray.ColumnStep)
                {
                    summ += swtImage.Pixels[i, j].StrokeWidth.Width;
                    ++pixelsNumberInRay;
                }

                return (double)summ / (double)pixelsNumberInRay;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Заполняет строку матрицы - изображения заданным значением ширины штриха
        /// </summary>
        /// <param name="image">Изображение</param>
        /// <param name="columnFrom">Номер столбца, от которого начинаем заполнять</param>
        /// <param name="columnTo">Номер столбца, на котором заканчиваем заполнять</param>
        /// <param name="row">Номер строки</param>
        /// <param name="strokeWidth">Ширина штриха</param>
        private void FillRowWithStrokeWidth(GreyImage image, int columnFrom, int columnTo, int row, int strokeWidth)
        {
            try
            {
                for (int i = columnFrom; i < columnTo; i++)
                {
                    double pixelStrokeWidth = image.Pixels[row, i].StrokeWidth.Width;
                    if (pixelStrokeWidth == StrokeWidthData.UNDEFINED_WIDTH || pixelStrokeWidth > strokeWidth)
                        image.Pixels[row, i].StrokeWidth.Width = strokeWidth;
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Заполняет столбец матрицы - изображения заданным значением ширины штриха
        /// </summary>
        /// <param name="image">Изображение</param>
        /// <param name="rowFrom">Номер строки, от которой начинаем заполнять</param>
        /// <param name="rowTo">Номер строки, на которой заканчиваем заполнять</param>
        /// <param name="coloumn">Номер столбца</param>
        /// <param name="strokeWidth">Ширина штриха</param>
        private void FillColumnWithStrokeWidth(GreyImage image, int rowFrom, int rowTo, int coloumn, int strokeWidth)
        {
            try
            {
                for (int i = rowFrom; i < rowTo; i++)
                {
                    double pixelStrokeWidth = image.Pixels[i, coloumn].StrokeWidth.Width;
                    if (pixelStrokeWidth == StrokeWidthData.UNDEFINED_WIDTH || pixelStrokeWidth > strokeWidth)
                        image.Pixels[i, coloumn].StrokeWidth.Width = strokeWidth;
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Заполняет диагональ матрицы - изображения заданным значением ширины штриха
        /// </summary>
        /// <param name="image">Изображение</param>
        /// <param name="rowFrom">Номер строки, от которой начинаем заполнять</param>
        /// <param name="rowTo">Номер строки, на которой заканчиваем заполнять</param>
        /// <param name="columnFrom">Номер столбца, от которого начинаем заполнять</param>         
        /// <param name="deltaColumn">Шаг по столбцу</param>
        /// <param name="strokeWidth">Ширина штриха</param>
        private void FillDiagonaWithStrokeWidth(GreyImage image, int rowFrom, int rowTo, int columnFrom, int deltaColumn, int strokeWidth)
        {
            try
            {
                for (int i = rowFrom, j = columnFrom; i < rowTo; i++, j += deltaColumn)
                {
                    double pixelStrokeWidth = image.Pixels[i, j].StrokeWidth.Width;
                    if (pixelStrokeWidth == StrokeWidthData.UNDEFINED_WIDTH || pixelStrokeWidth > strokeWidth)
                        image.Pixels[i, j].StrokeWidth.Width = strokeWidth;
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public override GreyImage Apply(GreyImage image, int threadsNumber)
        {
            throw new NotImplementedException();
        }

        protected override void ApplyThread(object data)
        {
            throw new NotImplementedException();
        }
    }
}
