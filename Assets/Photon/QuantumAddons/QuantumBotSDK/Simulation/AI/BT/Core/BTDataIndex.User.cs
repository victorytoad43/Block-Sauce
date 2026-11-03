namespace Quantum
{
    using System;

    [BotSDKHidden]
    [Serializable]
    // Used so we can track it easier on the Visual Editor
    public struct BTDataIndex
    {
        // This variable has been made public as, if private, Unity is having ussies when serializing it, causing it to reset for example when re-opening the editor
        // Users should not use this directly though
        /// <summary>
        /// Index used for internal purposes only. When the index is necessary, please use the <see cref="Index"/> property instead.
        /// </summary>
        public int _index;

        /// <summary>
        /// Sets the internal index of BTDataIndex, making it 1-based.<br/>
        /// <b>Caution: do not use this in user code. This is only used internally on the Bot SDK Compiler.</b>
        /// </summary>
        public void SetIndexCompilerOnly(int index)
        {
            _index = index + 1;
        }

        /// <summary>
        /// Returns the internal index, but 0-based for user code.
        /// </summary>
        public int Index
        {
            get
            {
                if (_index == 0)
                {
                    Log.Error("[Bot SDK] A Behaviour Tree node has a BTDataIndex instance with an invalid value. Please compile the used Behaviour Tree AI documents.");

                    return _index;
                }

                return _index - 1;
            }
        }
    }
}
