namespace BlockDataMap {

    //An interface for handling light data for non-solid blocks
    //Handles all the logic in fetching light values for a block
    public static class NonSolid
    {
        /// <summary>
        /// returns the block data as a byte between 0 and 15
        /// </summary>
        public static byte Light(Block block)
        {
            if (!block.controller.IsTransparent())
            {
                return 0;
            }

            if (block.controller.LightEmitted() != 0)
            {
                return block.controller.LightEmitted();
            }

            return (byte)block.data.GetData(0, 4);
        }

        /// <summary>
        /// sets the blocks data to a byte between 0 and 15
        /// </summary>
        public static byte Light(Block block, byte newValue)
        {
            if (!block.controller.IsTransparent())
            {
                return 0;
            }

            if (block.controller.LightEmitted() > newValue)
            {
                block.data.SetData(0, block.controller.LightEmitted(), 4);
                return block.controller.LightEmitted();
            }

            block.data.SetData(0, newValue, 4);
            return newValue;
        }
    }

    //An interface for handling crossmesh data
    public static class CrossMesh
    {
        // The data for crossmesh parameters is stored in nibbles. Nibbles are 4bit large 
        // variables that go between 0 and 15. We don't really need anything bigger than 16
        // so this works nicely and saves space. However the set and return values are all
        // abstracted into floats between 0 and 1 to make it easier for other code to use
        // this data without manipulating it. All the logic in fetching the value is here and
        // other code just gets and sets a simple float.

        /// <summary>
        /// Returns a float between 1 and 0 describing the height of the mesh
        /// </summary>
        public static float Height(Block block)
        {
            return block.data.GetData(4, 4) / 15f;
        }

        /// <summary>
        /// Sets the height of the mesh based on a new value between 1 and 0
        /// </summary>
        public static float Height(Block block, float newValue)
        {
            block.data.SetData(4, (byte)(newValue * 15), 4);
            return newValue;
        }

        /// <summary>
        /// Returns a float between 1 and 0 describing the XOffset of the mesh
        /// </summary>
        public static float XOffset(Block block)
        {
            return block.data.GetData(8, 4) / 15f;
        }

        /// <summary>
        /// Sets the XOffset of the mesh based on a new value between 1 and 0
        /// </summary>
        public static float XOffset(Block block, float newValue)
        {
            block.data.SetData(8, (byte)(newValue*15), 4);
            return newValue;
        }

        /// <summary>
        /// Returns a float between 1 and 0 describing the yOffset of the mesh
        /// </summary>
        public static float YOffset(Block block)
        {
            return block.data.GetData(12, 4) / 15f;
        }

        /// <summary>
        /// Sets the yOffset of the mesh based on a new value between 1 and 0
        /// </summary>
        public static float YOffset(Block block, float newValue)
        {
            block.data.SetData(12, (byte)(newValue * 15), 4);
            return newValue;
        }

    }
}
