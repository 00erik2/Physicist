﻿namespace Physicist.Actors
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.Xna.Framework;
    using Physicist.Controls;
    using Physicist.Events;

    public class PhysicistPath : PhysicistGameScreenItem
    {
        private Queue<PathNode> nodeQueue = new Queue<PathNode>();

        private Actor target = null;

        public PhysicistPath()
        {
        }

        public PhysicistPath(string name)
        {
            this.Name = name;
        }

        public event EventHandler PathCompleted;

        public Actor Target 
        {
            get
            {
                return this.target;
            }

            set
            {
                this.target = value;
                foreach (var node in this.nodeQueue)
                {
                    node.TargetActor = this.target;
                }
            }
        }

        public bool IsEnabled { get; set; }

        public bool LoopPath { get; set; }

        public string Name { get; set; }

        public string TargetPathUponCompletion { get; set; }

        public void AddPathNode(PathNode node)
        {
            if (node != null)
            {
                if (this.nodeQueue.Count == 0)
                {
                    node.Deactivated += this.NodeDeactivated;
                    node.IsActive = true;
                }

                this.nodeQueue.Enqueue(node);
            }
        }

        public void Update(GameTime gameTime)
        {
            if (this.IsEnabled && this.nodeQueue.Count > 0)
            {
                this.nodeQueue.ElementAt(0).Update(gameTime);
            }
        }

        public void ClearPathNodes()
        {
            this.nodeQueue.Clear();
        }

        public void NodeDeactivated(object sender, EventArgs e)
        {
            var pathNode = sender as PathNode;
            if (pathNode != null && this.nodeQueue.Contains(pathNode))
            {
                this.ChangeNodes();
            }
        }

        public void ChangeNodes()
        {
            var oldNode = this.nodeQueue.Dequeue();
            if (oldNode != null)
            {
                oldNode.Deactivated -= this.NodeDeactivated;
                if (this.LoopPath)
                {
                    this.nodeQueue.Enqueue(oldNode);
                }
            }

            if (this.nodeQueue.Count > 0)
            {
                var nextNode = this.nodeQueue.ElementAt(0);
                nextNode.Deactivated += this.NodeDeactivated;
                nextNode.IsActive = true;
            }
            else
            {
                this.IsEnabled = false;
                this.PathHasCompleted();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return this.nodeQueue.GetEnumerator();
        }

        public override XElement XmlSerialize()
        {
            throw new NotImplementedException();
        }

        public override void XmlDeserialize(XElement element)
        {
            if (element != null)
            {
                this.Name = element.Attribute("name").Value;

                this.IsEnabled = true;
                var isEnabledAtt = element.Attribute("isEnabled");
                if (isEnabledAtt != null)
                {
                    this.IsEnabled = bool.Parse(isEnabledAtt.Value);
                }

                this.LoopPath = true;
                var loopPathAtt = element.Attribute("loopPath");
                if (loopPathAtt != null)
                {
                    this.LoopPath = bool.Parse(loopPathAtt.Value);
                }

                var targetPathUponComplteionEle = element.Attribute("targetPathUponCompletion");
                if (targetPathUponComplteionEle != null)
                {
                    this.TargetPathUponCompletion = targetPathUponComplteionEle.Value;
                }

                var modifiers = new List<IModifier>();
                var modifierEleList = element.Element("Modifiers");
                if (modifierEleList != null)
                {
                    foreach (var modifierEle in modifierEleList.Elements())
                    {
                        IModifier modifier = (IModifier)MapLoader.CreateInstance(modifierEle, "class");
                        modifier.XmlDeserialize(modifierEle);
                        modifiers.Add(modifier);
                    }
                }

                foreach (var pathNodeEle in element.Elements().Where(elem => elem.Name != "Modifiers"))
                {
                    PathNode node = (PathNode)MapLoader.CreateInstance(pathNodeEle, null);
                    node.Screen = this.Screen;
                    node.XmlDeserialize(pathNodeEle);
                    if (node != null)
                    {
                        node.TargetActor = this.target;
                        node.Initialize(modifiers);
                        this.AddPathNode(node);
                    }
                }                
            }
        }

        private void PathHasCompleted()
        {
            if (this.PathCompleted != null)
            {
                this.PathCompleted(this, null);
            }
        }
    }
}
