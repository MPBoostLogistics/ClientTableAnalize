using System.Drawing;

namespace ClientTableAnalizer.Interfaces
{
    public interface IColorable 
    {
        public Color FillColor { get; set;}

        public void SetColor(Color fillColor) => FillColor = fillColor;
    }
}