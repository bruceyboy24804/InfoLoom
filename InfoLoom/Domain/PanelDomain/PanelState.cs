namespace InfoLoomTwo.Domain
{
    public class PanelState
    {
        private string m_Id;
        private Position m_Position;
        private Size m_Size;

        /// <summary>
        /// Initializes a new instance of <see cref="PanelState"/>
        /// </summary>
        public PanelState()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PanelState"/>
        /// </summary>
        /// <param name="id">string id for panel</param>
        /// <param name="position">Position top and left.</param>
        /// <param name="size">Size height and width.</param>
        public PanelState(string id, Position position, Size size)
        {
            m_Id = id;
            m_Position = position;
            m_Size = size;
        }

        /// <summary>
        /// Gets or sets the value of ID.
        /// </summary>
        public string Id { get { return m_Id; } set { m_Id = value; } }

        /// <summary>
        /// Gets or set the values of the position.
        /// </summary>
        public Position Position { get { return m_Position; } set { m_Position = value; } }

        /// <summary>
        /// Gets or sets the values for size.
        /// </summary>
        public Size Size { get { return m_Size; } set { m_Size = value; } }
    }
}