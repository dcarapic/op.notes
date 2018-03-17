using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OP.Notes
{
    public static class WpfExtensions
    {

        public static FluentAnimationHelper<T> AnimateTranslateTransform<T>(this T element, Point from, Point to, TimeSpan duration, bool clearAnimationWhenFinished) where T : UIElement
        {
            TranslateTransform transform = null;
            if (element.RenderTransform == null)
            {
                var tg = new TransformGroup();
                transform = new TranslateTransform();
                tg.Children.Add(tg);
                element.RenderTransform = tg;
            }
            else if(element.RenderTransform is TranslateTransform)
            {
                transform = (TranslateTransform)element.RenderTransform;
            }
            else if (element.RenderTransform is TransformGroup)
            {
                var tg = (TransformGroup)element.RenderTransform;
                transform = (TranslateTransform)tg.Children.FirstOrDefault(c => c is TranslateTransform);
                if (transform == null)
                {
                    transform = new TranslateTransform();
                    tg.Children.Add(tg);
                }
            }
            if (transform == null)
                throw new InvalidOperationException("Element has unsupported RenderTransform. Can not AnimateMove.");
            var helper = new FluentAnimationHelper<T>(element, transform, clearAnimationWhenFinished);
            
            DoubleAnimation animX = new DoubleAnimation();
            animX.From = from.X;
            animX.To = to.X;
            animX.Duration = duration;
            bool animXCompleted = false;

            DoubleAnimation animY = new DoubleAnimation();
            animY.From = from.Y;
            animY.To = to.Y;
            animY.Duration = duration;
            bool animYCompleted = false;
            
            animX.Completed += (s, e1) =>
            {
                animXCompleted = true;
                if (!animYCompleted)
                    return;
                helper.ExecuteThen();
            };
            animX.Completed += (s, e1) =>
            {
                animYCompleted = true;
                if (!animXCompleted)
                    return;
                helper.ExecuteThen();
            };
            transform.BeginAnimation(TranslateTransform.XProperty, animX);
            transform.BeginAnimation(TranslateTransform.YProperty, animY);
            return helper;
        }


        public class FluentAnimationHelper<T> where T : UIElement
        {
            Action _action1;
            Action<T> _action2;
            T _element;
            TranslateTransform _transform;
            bool _shouldClearAnimation;


            internal FluentAnimationHelper(T element, TranslateTransform transform, bool clearAnimation)
            {
                _element = element;
                _transform = transform;
                _shouldClearAnimation = clearAnimation;
            }

            public void Then(Action action)
            {
                _action1 = action;
            }

            public void Then(Action<T> action)
            {
                _action2 = action;
            }


            internal void ExecuteThen()
            {
                if (_action1 != null)
                    _action1();
                else if (_action2 != null)
                    _action2(_element);

                if (_shouldClearAnimation)
                {
                    _transform.ApplyAnimationClock(TranslateTransform.XProperty, null);
                    _transform.ApplyAnimationClock(TranslateTransform.YProperty, null);
                }
            }
        }


    }
}
