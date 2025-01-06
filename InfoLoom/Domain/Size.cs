
namespace InfoLoomTwo.Domain
{
    /// <summary>
    /// A class for transfering size data to and from the UI.
    /// </summary>
    public class Size
    {
        private float m_Width;
        private float m_Height;

        /// <summary>
        /// Gets or sets the top value.
        /// </summary>
        public float width
        {
            get { return m_Width; }
            set { m_Width = value; }
        }

        /// <summary>
        /// Gets or sets the bottom value.
        /// </summary>
        public float height
        {
            get { return m_Height; }
            set { m_Height = value; }
        }

        /// <summary>
        /// Initializes a Size with width and height.
        /// </summary>
        /// <param name="width">Width of panel.</param>
        /// <param name="height">Height of panel.</param>
        public Size (float width, float height)
        {
            m_Width = width;
            m_Height = height;
        }
    }
}
