using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OP.Notes.Controls
{
    public class NotesEditor : TextEditor
    {

        private Regex _regexWordHiglight;
        private bool _trackingCaret;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            TextArea.TextView.ElementGenerators.Add(new CheckboxElementGenerator(this));
            TextArea.TextView.ElementGenerators.Add(new ImageElementGenerator(this));
            TextArea.TextView.LineTransformers.Add(new ColorizeAvalonEdit(this));
            var pasteCommand = TextArea.CommandBindings.Cast<CommandBinding>().FirstOrDefault(cb => cb.Command == ApplicationCommands.Paste);
            if (pasteCommand != null)
            {
                pasteCommand.PreviewCanExecute += pasteCommand_PreviewCanExecute;
            }
            this.RequestBringIntoView += NotesEditor_RequestBringIntoView;
            this.TextArea.SelectionCornerRadius = 0;
            this.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        }

        void Caret_PositionChanged(object sender, EventArgs e)
        {
            var caretRect = this.TextArea.Caret.CalculateCaretRectangle();
            caretRect.Inflate(0, caretRect.Height * 1.5); // make it bigger
            if (!IsRectangleVisible(this, caretRect))
            {
                _trackingCaret = true;
                this.BringIntoView(caretRect);
            }
        }

        public bool IsRectangleVisible(FrameworkElement element, Rect caretRect)
        {
            var container = VisualTreeHelper.GetParent(element) as FrameworkElement;
            if (container == null) return true;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(caretRect);
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            if (!rect.IntersectsWith(bounds))
                return IsRectangleVisible(container, bounds);
            else
                return false;
        }

        void NotesEditor_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // prevent scrollviewer to automaticaly adjust itself to editor
            if (_trackingCaret)
            {
                _trackingCaret = false;
                return;
            }
            e.Handled = true;
        }

        

        void pasteCommand_PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Source == TextArea)
            {
                var bitmap = Clipboard.GetImage();
                if (bitmap == null)
                    return;
                if (ImageFolder == null)
                    return;

                var args = new ImagePasteEventArgs(bitmap);
                args.ImageFileName = "Image_" + DateTime.Now.ToString("yyyy_MM_dd_hh_ss");
                var handler = ImagePasting;
                if (handler != null)
                {
                    ImagePasting(this, args);
                    if (args.Cancel)
                        return;
                }
                HandlePastedImage(args.Image, args.ImageFileName);
            }
        }



        public event EventHandler<ImagePasteEventArgs> ImagePasting;


        /// <summary>
        /// Gets or sets the checkbox style.
        /// </summary>
        public Style CheckboxStyle { get; set; }


        public Regex HighlightWords
        {
            get { return _regexWordHiglight; }
            set
            {
                if (_regexWordHiglight == value)
                    return;
                _regexWordHiglight = value;
                TextArea.TextView.Redraw();
            }
        }

        public string ImageFolder
        {
            get;
            set;
        }


        private void HandlePastedImage(BitmapSource image, string fileName)
        {
            var fullFileName = System.IO.Path.Combine(ImageFolder, fileName) + ".jpg";
            using (var fileStream = new FileStream(fullFileName, FileMode.Create))
            {
                BitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
            Document.Insert(CaretOffset, "![image](" + fileName + ".jpg)");
        }
    }

    public class ImagePasteEventArgs : System.ComponentModel.CancelEventArgs
    {
        /// <summary>
        /// Gets the image.
        /// </summary>
        public BitmapSource Image { get; private set; }

        /// <summary>
        /// Gets or sets the filename under which the image will be saved.
        /// </summary>
        public string ImageFileName { get; set; }

        public ImagePasteEventArgs(BitmapSource image)
        {
            Image = image;
        }
    }

    public class ColorizeAvalonEdit : DocumentColorizingTransformer
    {

        private Regex _regexBold = new Regex(@"\*.+?\*", RegexOptions.Compiled);
        private Regex _regexLarge = new Regex(@"^#", RegexOptions.Compiled);

        private NotesEditor _editor;

        public ColorizeAvalonEdit(NotesEditor editor)
        {
            _editor = editor;
        }


        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            var matches = _regexBold.Matches(text);
            foreach (Match match in matches)
            {
                base.ChangeLinePart(lineStartOffset + match.Index, lineStartOffset + match.Index + match.Length, (VisualLineElement element) =>
                    {
                        Typeface tf = element.TextRunProperties.Typeface;
                        element.TextRunProperties.SetForegroundBrush(Brushes.Maroon);
                        element.TextRunProperties.SetTypeface(new Typeface(
                            tf.FontFamily,
                            FontStyles.Normal,
                            FontWeights.Bold,
                            tf.Stretch
                        ));
                    });
            }
            if (_editor.HighlightWords != null)
            {
                matches = _editor.HighlightWords.Matches(text);
                foreach (Match match in matches)
                {
                    base.ChangeLinePart(lineStartOffset + match.Index, lineStartOffset + match.Index + match.Length, (VisualLineElement element) =>
                    {
                        element.TextRunProperties.SetBackgroundBrush(Brushes.Yellow);
                    });
                }
            }
            if (text.StartsWith("#"))
                base.ChangeLinePart(lineStartOffset + 0, lineStartOffset + text.Length, (VisualLineElement element) =>
                {
                    element.TextRunProperties.SetFontRenderingEmSize(element.TextRunProperties.FontRenderingEmSize * 1.4);
                });

        }
    }

    public class CheckboxElementGenerator : VisualLineElementGenerator
    {
        private NotesEditor _editor;

        public CheckboxElementGenerator(NotesEditor editor)
        {
            _editor = editor;
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            var text = CurrentContext.Document.GetText(startOffset, CurrentContext.VisualLine.LastDocumentLine.EndOffset - startOffset);
            var cbIndex = text.IndexOf("[]");
            var cbcIndex = text.IndexOf("[x]");
            var offset = -1;
            if (cbIndex == -1)
                offset = cbcIndex;
            else if (cbcIndex == -1)
                offset = cbIndex;
            else
                offset = (cbIndex < cbcIndex ? cbIndex : cbcIndex);
            if (offset == -1)
                return offset;
            else
                return offset + startOffset;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            var nextChar = CurrentContext.Document.GetText(offset + 1, 1);
            CheckBox c = new CheckBox();
            c.Cursor = Cursors.Hand;
            c.Margin = new Thickness(5, 0, 5, 0);
            c.Style = _editor.CheckboxStyle;
            c.Focusable = false;
            c.RenderTransform = new TranslateTransform(0, 3);

            int length = 2;
            if (nextChar != "]")
            {
                c.IsChecked = true;
                length += 1;
            }
            c.Checked += c_Checked;
            c.Unchecked += c_Checked;
            c.Tag = offset;
            var element = new InlineObjectElement(length, c);
            return element;
        }

        void c_Checked(object sender, RoutedEventArgs e)
        {

            CheckBox c = (CheckBox)sender;
            var offset = c.TranslatePoint(new Point(0, 0), _editor);
            var editorPosition = _editor.GetPositionFromPoint(new Point(offset.X, offset.Y));
            var textPosition = _editor.Document.GetOffset(editorPosition.Value.Line, editorPosition.Value.Column);
            var text = _editor.Document.GetText(textPosition, 4);
            if (c.IsChecked.GetValueOrDefault())
                _editor.Document.Insert(textPosition + 1, "x");
            else
                _editor.Document.Remove(textPosition + 1, 1);
            _editor.Focus();
        }

    }


    public class ImageElementGenerator : VisualLineElementGenerator
    {
        private NotesEditor _editor;
        private Regex _regexImage = new Regex(@"", RegexOptions.Compiled);
        private static ImageCache _cache = new ImageCache();
        private List<Image> _imagesToResize = new List<Image>();
        private DispatcherTimer _resizingTimer;

        public ImageElementGenerator(NotesEditor editor)
        {
            _editor = editor;
            _resizingTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, ResizeImages, _editor.Dispatcher);
        }

        private void ResizeImages(object sender, EventArgs e)
        {
            foreach (var image in _imagesToResize)
            {
                if (image.Width != _editor.ActualWidth)
                    image.Width = _editor.ActualWidth;
            }
            _imagesToResize.Clear();
            _resizingTimer.Stop();
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            var text = CurrentContext.Document.GetText(startOffset, CurrentContext.VisualLine.LastDocumentLine.EndOffset - startOffset);
            var imgIndex = text.IndexOf("![image](");
            if (imgIndex == -1)
                return -1;
            else if (text.IndexOf(")", imgIndex) < 0)
                return -1;
            else
                return imgIndex + startOffset;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            _resizingTimer.Stop();
            var text = CurrentContext.Document.GetText(offset, CurrentContext.VisualLine.LastDocumentLine.EndOffset - offset);
            var startOffset = "![image](".Length;
            var endOffset = text.IndexOf(")");

            var imageFile = text.Substring(startOffset, endOffset - startOffset);
            Image i = new Image();
            i.Focusable = false;
            var b = _cache.GetImage(_editor.ImageFolder, imageFile);
            if (b != null)
                i.Source = b;
            i.Stretch = Stretch.UniformToFill;
            i.StretchDirection = StretchDirection.DownOnly;
            i.HorizontalAlignment = HorizontalAlignment.Left;
            i.VerticalAlignment = VerticalAlignment.Top;
            i.Width = _editor.ActualWidth;
            System.Diagnostics.Debug.Print("Creating image: " + imageFile);
            //Binding widthBinding = new Binding();
            //widthBinding.FallbackValue = (double)200;
            //widthBinding.Source = _editor;
            //widthBinding.Mode = BindingMode.OneWay;
            //widthBinding.Path = new PropertyPath("ActualWidth");
            //widthBinding.IsAsync = true;
            //i.SetBinding(Image.WidthProperty, widthBinding);
            var images = _imagesToResize;
            images.Add(i);
            i.Unloaded += (s,e) => images.Remove(i);
            var element = new InlineObjectElement(endOffset + 1, i);
            _resizingTimer.Start();
            return element;
        }

        void c_Checked(object sender, RoutedEventArgs e)
        {

            CheckBox c = (CheckBox)sender;
            var offset = c.TranslatePoint(new Point(0, 0), _editor);
            var editorPosition = _editor.GetPositionFromPoint(new Point(offset.X, offset.Y));
            var textPosition = _editor.Document.GetOffset(editorPosition.Value.Line, editorPosition.Value.Column);
            var text = _editor.Document.GetText(textPosition, 4);
            if (c.IsChecked.GetValueOrDefault())
                _editor.Document.Insert(textPosition + 1, "x");
            else
                _editor.Document.Remove(textPosition + 1, 1);
            _editor.Dispatcher.BeginInvoke(new Action(() =>
            {
                Keyboard.Focus(_editor);
            }));

        }


    }

    public class ImageCache
    {
        private Dictionary<string, Dictionary<string, BitmapImage>> _imageCache = new Dictionary<string,Dictionary<string,BitmapImage>>();

        public BitmapImage GetImage(string imageFolder, string imageName)
        {
            if(!_imageCache.ContainsKey(imageFolder))
                _imageCache[imageFolder] = new Dictionary<string,BitmapImage>();
            if (_imageCache[imageFolder].ContainsKey(imageName))
                return _imageCache[imageFolder][imageName];
            try
            {
                var b = new BitmapImage();
                b.BeginInit();
                var imageFileName = System.IO.Path.Combine(imageFolder, imageName);
                if (File.Exists(imageFileName))
                {
                    if (System.IO.Path.IsPathRooted(imageFolder))
                        b.UriSource = new Uri(imageFileName, UriKind.Absolute);
                    else
                        b.UriSource = new Uri(imageFileName, UriKind.Relative);

                    b.CacheOption = BitmapCacheOption.OnLoad;
                    b.EndInit();
                    b.Freeze();
                    _imageCache[imageFolder][imageName] = b;
                    return b;
                }
                else
                {
                    _imageCache[imageFolder][imageName] = null;
                    return null;
                }
                
            }
            catch (Exception) { } // ignore invalid image
            return null;
        }
    }
}
