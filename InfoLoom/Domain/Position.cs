
namespace InfoLoomTwo.Domain
{
    /// <summary>
    /// A class for transfering position data to and from the UI.
    /// </summary>
    public class Position
    {
        private float m_Top;
        private float m_Left;

        /// <summary>
        /// Initializes the position with top and left values.
        /// </summary>
        /// <param name="top">A float value for position from top.</param>
        /// <param name="left">A float value for position form left.</param>
        public Position(float top, float left)
        {
            m_Top = top;
            m_Left = left;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Position"/>
        /// </summary>
        public Position()
        {
        }

        /// <summary>
        /// Gets or sets the top value.
        /// </summary>
        public float top
        {
            get { return m_Top; }
            set { m_Top = value; }
        }

        /// <summary>
        /// Gets or sets the bottom value.
        /// </summary>
        public float left
        {
            get { return m_Left; }
            set { m_Left = value; }
        }
    }
}
