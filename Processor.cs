using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Accord.Imaging;
using Accord.Imaging.Filters;

namespace Accord.WindowsForms
{
    internal class Settings
    {
        private int _border = 20;
        public int border
        {
            get
            {
                return _border;
            }
            set
            {
                if ((value > 0) && (value < height / 3))
                {
                    _border = value;
                    if (top > 2 * _border) top = 2 * _border;
                    if (left > 2 * _border) left = 2 * _border;
                }
            }
        }

        public int width = 640;
        public int height = 640;
        
        /// <summary>
        /// Размер сетки для сенсоров по горизонтали
        /// </summary>
        public int blocksCount = 10;

        /// <summary>
        /// Желаемый размер изображения до обработки
        /// </summary>
        public Size orignalDesiredSize = new Size(500, 500);
        /// <summary>
        /// Желаемый размер изображения после обработки
        /// </summary>
        public Size processedDesiredSize = new Size(500, 500);

        public int margin = 10;
        public int top = 40;
        public int left = 40;

        /// <summary>
        /// Второй этап обработки
        /// </summary>
        public bool processImg = false;

        /// <summary>
        /// Порог при отсечении по цвету 
        /// </summary>
        public byte threshold = 120;
        public float differenceLim = 0.15f;

        public void incTop() { if (top < 2 * _border) ++top; }
        public void decTop() { if (top > 0) --top; }
        public void incLeft() { if (left < 2 * _border) ++left; }
        public void decLeft() { if (left > 0) --left; }

        public bool hasSensorValues = false;
    }

    internal class MagicEye
    {
        /// <summary>
        /// Обработанное изображение
        /// </summary>
        public Bitmap processed;
        /// <summary>
        /// Оригинальное изображение после обработки
        /// </summary>
        public Bitmap original;

        /// <summary>
        /// Класс настроек
        /// </summary>
        public Settings settings = new Settings();

        /// <summary>
        /// Показания сенсоров
        /// </summary>
        public double[] sensors;
        public MagicEye()
        {
        }

        public bool ProcessImage(Bitmap bitmap)
        {
            // На вход поступает необработанное изображение с веб-камеры

            //  Минимальная сторона изображения (обычно это высота)
            if (bitmap.Height > bitmap.Width)
                throw new Exception("К такой забавной камере меня жизнь не готовила!");
            //  Можно было, конечено, и не кидаться эксепшенами в истерике, но идите и купите себе нормальную камеру!
            int side = bitmap.Height;

            //  Отпиливаем границы, но не более половины изображения
            if (side < 4 * settings.border) settings.border = side / 4;
            side -= 2 * settings.border;
            
            //  Мы сейчас занимаемся тем, что красиво оформляем входной кадр, чтобы вывести его на форму
            Rectangle cropRect = new Rectangle((bitmap.Width - bitmap.Height) / 2 + settings.left + settings.border, settings.top + settings.border, side, side);
            
            //  Тут создаём новый битмапчик, который будет исходным изображением
            original = new Bitmap(cropRect.Width, cropRect.Height);

            //  Объект для рисования создаём
            Graphics g = Graphics.FromImage(original);
            
            g.DrawImage(bitmap, new Rectangle(0, 0, original.Width, original.Height), cropRect, GraphicsUnit.Pixel);
            Pen p = new Pen(Color.Red);
            p.Width = 1;

            //  Теперь всю эту муть пилим в обработанное изображение
            Accord.Imaging.Filters.Grayscale grayFilter = new Accord.Imaging.Filters.Grayscale(0.2125, 0.7154, 0.0721);
            var uProcessed = grayFilter.Apply(Accord.Imaging.UnmanagedImage.FromManagedImage(original));

            
            int blockWidth = original.Width / settings.blocksCount;
            int blockHeight = original.Height / settings.blocksCount;
            for (int r = 0; r < settings.blocksCount; ++r)
                for (int c = 0; c < settings.blocksCount; ++c)
                {
                    //  Тут ещё обработку сделать
                    g.DrawRectangle(p, new Rectangle(c * blockWidth, r * blockHeight, blockWidth, blockHeight));
                }


            //  Масштабируем изображение до 500x500 - этого достаточно
            Accord.Imaging.Filters.ResizeBilinear scaleFilter = new Accord.Imaging.Filters.ResizeBilinear(settings.orignalDesiredSize.Width, settings.orignalDesiredSize.Height);
            uProcessed = scaleFilter.Apply(uProcessed);
            original = scaleFilter.Apply(original);
            g = Graphics.FromImage(original);
            //  Пороговый фильтр применяем. Величина порога берётся из настроек, и меняется на форме
            Accord.Imaging.Filters.BradleyLocalThresholding threshldFilter = new Accord.Imaging.Filters.BradleyLocalThresholding();
            threshldFilter.PixelBrightnessDifferenceLimit = settings.differenceLim;
            threshldFilter.ApplyInPlace(uProcessed);

/*            Accord.Imaging.BlobCounter blobCounter = new Accord.Imaging.BlobCounter
            {
                FilterBlobs = true,
                MinHeight = 5,
                MinWidth = 5,
                ObjectsOrder = Accord.Imaging.ObjectsOrder.Area
            };
            blobCounter.ProcessImage(uProcessed);

            Accord.Imaging.Blob[] blobs = blobCounter.GetObjectsInformation();

            if (blobs.Length > 0)
            {
                // Assuming we work with the largest blob
                Accord.Imaging.Blob largestBlob = blobs[0];
            }*/


            if (settings.processImg)
            {

                string info = processSample(ref uProcessed);
                Font f = new Font(FontFamily.GenericSansSerif, 20);
                g.DrawString(info, f, Brushes.Black, 30, 30);

                double[] transitionsVector = new double[200];

                // Подсчет переходов по строкам
                for (int y = 0; y < 100; y++)
                {
                    bool lastPixelBlack = uProcessed.GetPixel(0, y).R < 128;
                    for (int x = 1; x < 100; x++)
                    {
                        bool currentPixelBlack = uProcessed.GetPixel(x, y).R < 128;
                        if (currentPixelBlack != lastPixelBlack)
                        {
                            transitionsVector[y]++;
                            lastPixelBlack = currentPixelBlack;
                        }
                    }
                }

                // Подсчет переходов по столбцам
                for (int x = 0; x < 100; x++)
                {
                    bool lastPixelBlack = uProcessed.GetPixel(x, 0).R < 128;
                    for (int y = 1; y < 100; y++)
                    {
                        bool currentPixelBlack = uProcessed.GetPixel(x, y).R < 128;
                        if (currentPixelBlack != lastPixelBlack)
                        {
                            transitionsVector[100 + x]++;
                            lastPixelBlack = currentPixelBlack;
                        }
                    }
                }

                sensors = transitionsVector;
                settings.hasSensorValues = true;
            }



            //  Можно вывести информацию на изображение!
            //Font f = new Font(FontFamily.GenericSansSerif, 10);
            //for (int r = 0; r < 4; ++r)
            //    for (int c = 0; c < 4; ++c)
            //        if (currentDeskState[r * 4 + c] >= 1 && currentDeskState[r * 4 + c] <= 16)
            //        {
            //            int num = 1 << currentDeskState[r * 4 + c];
            //            
            //        }


            processed = uProcessed.ToManagedImage();

            return true;
        }

        /// <summary>
        /// Обработка одного сэмпла
        /// </summary>
        /// <param name="index"></param>
        private string processSample(ref Imaging.UnmanagedImage unmanaged)
        {
            string rez = "Обработка";

            ///  Инвертируем изображение
            Accord.Imaging.Filters.Invert InvertFilter = new Accord.Imaging.Filters.Invert();
            InvertFilter.ApplyInPlace(unmanaged);

            ///    Создаём BlobCounter, выдёргиваем самый большой кусок, масштабируем, пересечение и сохраняем
            ///    изображение в эксклюзивном использовании
            Accord.Imaging.BlobCounterBase bc = new Accord.Imaging.BlobCounter();

            bc.FilterBlobs = true;
            bc.MinWidth = 3;
            bc.MinHeight = 3;
            // Упорядочиваем по размеру
            bc.ObjectsOrder = Accord.Imaging.ObjectsOrder.Size;
            // Обрабатываем картинку

            bc.ProcessImage(unmanaged);
            if (bc.GetObjectsInformation().Length > 0)
            {
                Rectangle[] rects = bc.GetObjectsRectangles();

                /*            
                            rez = "Насчитали " + rects.Length.ToString() + " прямоугольников!";*/
                //if (rects.Length == 0)
                //{
                //    finalPics[r, c] = Accord.Imaging.UnmanagedImage.FromManagedImage(new Bitmap(100, 100));
                //    return 0;
                //}

                // К сожалению, код с использованием подсчёта blob'ов не работает, поэтому просто высчитываем максимальное покрытие
                // для всех блобов - для нескольких цифр, к примеру, 16, можем получить две области - отдельно для 1, и отдельно для 6.
                // Строим оболочку, включающую все блоки. Решение плохое, требуется доработка
                int lx = unmanaged.Width;
                int ly = unmanaged.Height;
                int rx = 0;
                int ry = 0;
                for (int i = 0; i < rects.Length; ++i)
                {
                    if (lx > rects[i].X) lx = rects[i].X;
                    if (ly > rects[i].Y) ly = rects[i].Y;
                    if (rx < rects[i].X + rects[i].Width) rx = rects[i].X + rects[i].Width;
                    if (ry < rects[i].Y + rects[i].Height) ry = rects[i].Y + rects[i].Height;
                }

                // Обрезаем края, оставляя только центральные блобчики
                Accord.Imaging.Filters.Crop cropFilter = new Accord.Imaging.Filters.Crop(new Rectangle(lx, ly, rx - lx, ry - ly));
                unmanaged = cropFilter.Apply(unmanaged);

                // Поворот не туда
                /*            if (bc.GetObjectsInformation().Length > 0)
                            {
                                var largestBlob = bc.GetObjectsInformation()[0];

                                var rawMoments = new Accord.Imaging.Moments.RawMoments();

                                rawMoments.Compute(unmanaged, largestBlob.Rectangle);

                                var centralMoments = new Accord.Imaging.Moments.CentralMoments();
                                centralMoments.Compute(rawMoments);

                                var angle = centralMoments.GetOrientation();

                                rez = $"Angle: {angle}";

                                RotateBilinear rotationFilter = new RotateBilinear(-angle * (180 / System.Math.PI), true);
                                unmanaged = rotationFilter.Apply(unmanaged);
                            }*/
            }
            GaussianBlur gaussianBlur = new GaussianBlur(sigma: 2.0);
            unmanaged = gaussianBlur.Apply(unmanaged);

            Median medianFilter = new Median();
            unmanaged = medianFilter.Apply(unmanaged);


            //  Масштабируем до 100x100
            Accord.Imaging.Filters.ResizeBilinear scaleFilter = new Accord.Imaging.Filters.ResizeBilinear(100, 100);
            unmanaged = scaleFilter.Apply(unmanaged);

            return rez;
        }

    }
}

