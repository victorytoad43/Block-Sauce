using System;

namespace Quantum
{
    [Serializable]
    public unsafe partial class BTRoot : BTDecorator
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        /// <summary>
        /// The total amount of nodes on the BT.
        /// </summary>
        [BotSDKHidden] public Int32 NodesAmount;

        /// <summary>
        /// The total amount of nodes which use <see cref="BTDataIndex"/> to store node-specific data in <see cref="BTAgent.BTDataValues"/>.
        /// </summary>
        [BotSDKHidden] public Int32 NodeDataAmount;

        // ========== BTDecorator INTERFACE ===========================================================================

        public override BTNodeType NodeType
        {
            get
            {
                return BTNodeType.Root;
            }
        }

        protected unsafe override BTStatus OnUpdate(BTParams btParams, ref AIContext aiContext)
        {
            btParams.Agent->Current = this;

            if (_childInstance != null)
            {
                return _childInstance.Execute(btParams, ref aiContext);
            }

            return BTStatus.Success;
        }

        // ========== PUBLIC METHODS ==================================================================================

        public void InitializeTree(BTParams btParams, ref AIContext aiContext)
        {
            InitNodesRecursively(this, btParams, ref aiContext);
        }

        // ========== PRIVATE METHODS =================================================================================

        private static void InitNodesRecursively(BTNode node, BTParams btParams, ref AIContext aiContext)
        {
            node.InitInternal(btParams, ref aiContext);
            node.Init(btParams, ref aiContext);

            if (node is BTDecorator decoratorNode)
            {
                BTNode childNode = btParams.Frame.FindAsset<BTNode>(decoratorNode.Child.Id);
                InitNodesRecursively(childNode, btParams, ref aiContext);
            }

            if (node is BTComposite compositeNode)
            {
                foreach (var child in compositeNode.Children)
                {
                    BTNode childNode = btParams.Frame.FindAsset<BTNode>(child.Id);
                    InitNodesRecursively(childNode, btParams, ref aiContext);
                }
            }
        }
    }
}