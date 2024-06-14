using System.Drawing;

namespace TableAnalizer.Interfaces
{
    public interface IColorable 
    {
        public Color FillColor { get; set;}

        public void SetColor(Color fillColor) => FillColor = fillColor;
    }
}