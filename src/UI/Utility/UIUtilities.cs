using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public static class UIUtilities
    {
        public static Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            return Sprite.Create(texture,
                                 new Rect(0.0f, 0.0f, texture.width, texture.height),
                                 Vector2.zero);
        }

        public static void OpenYouTubeVideoURL(string youTubeVideoId)
        {
            if(!String.IsNullOrEmpty(youTubeVideoId))
            {
                Application.OpenURL(@"https://youtu.be/" + youTubeVideoId);
            }
        }

        /// <summary>Counts the cells that will fit in within the RectTransform of the given grid</summary>
        public static int CountVisibleGridCells(GridLayoutGroup gridLayout)
        {
            Debug.Assert(gridLayout != null);

            // calculate dimensions
            RectTransform transform = gridLayout.GetComponent<RectTransform>();
            Vector2 gridDisplayDimensions = new Vector2();
            gridDisplayDimensions.x = (transform.rect.width
                                       - gridLayout.padding.left
                                       - gridLayout.padding.right
                                       + gridLayout.spacing.x);
            gridDisplayDimensions.y = (transform.rect.height
                                       - gridLayout.padding.top
                                       - gridLayout.padding.bottom
                                       + gridLayout.spacing.y);

            // calculate cell count
            int columnCount = 0;
            if(gridLayout.cellSize.x + gridLayout.spacing.x > 0f)
            {
                columnCount = (int)Mathf.Floor(gridDisplayDimensions.x
                                               / (gridLayout.cellSize.x + gridLayout.spacing.x));

            }
            int rowCount = 0;
            if((gridLayout.cellSize.y + gridLayout.spacing.y) > 0f)
            {
                rowCount = (int)Mathf.Floor(gridDisplayDimensions.y
                                            / (gridLayout.cellSize.y + gridLayout.spacing.y));
            }

            // check constraints
            if(gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                if(gridLayout.constraintCount < columnCount)
                {
                    columnCount = gridLayout.constraintCount;
                }
            }
            else if(gridLayout.constraint == GridLayoutGroup.Constraint.FixedRowCount)
            {
                if(gridLayout.constraintCount < rowCount)
                {
                    rowCount = gridLayout.constraintCount;
                }
            }

            return rowCount * columnCount;
        }

        /// <summary>Finds the first instance of a component in any loaded scenes.</summary>
        public static T FindComponentInAllScenes<T>(bool includeInactive)
        where T : Behaviour
        {
            foreach(T component in Resources.FindObjectsOfTypeAll<T>())
            {
                if(component.hideFlags == HideFlags.NotEditable
                    || component.hideFlags == HideFlags.HideAndDontSave)
                {
                    continue;
                }

                #if UNITY_EDITOR
                if(UnityEditor.EditorUtility.IsPersistent(component.transform.root.gameObject))
                {
                    continue;
                }
                #endif

                if(includeInactive
                   || component.isActiveAndEnabled)
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>Finds the instances of a component in any loaded scenes.</summary>
        public static List<T> FindComponentsInAllScenes<T>(bool includeInactive)
        where T : Behaviour
        {

            List<T> sceneComponents = new List<T>();

            foreach(T component in Resources.FindObjectsOfTypeAll<T>())
            {
                if(component.hideFlags == HideFlags.NotEditable
                    || component.hideFlags == HideFlags.HideAndDontSave)
                {
                    continue;
                }

                #if UNITY_EDITOR
                if(UnityEditor.EditorUtility.IsPersistent(component.transform.root.gameObject))
                {
                    continue;
                }
                #endif

                if(includeInactive
                   || component.isActiveAndEnabled)
                {
                    sceneComponents.Add(component);
                }
            }

            return sceneComponents;
        }

        /// <summary>Creates/Destroys a number of GameObject instances as necessary.</summary>
        public static void SetInstanceCount<T>(Transform container, T template,
                                               string instanceName, int instanceCount,
                                               ref T[] instanceArray, bool reactivateAll = false)
        where T : MonoBehaviour
        {
            if(instanceArray == null)
            {
                instanceArray = new T[0];
            }

            int difference = instanceCount - instanceArray.Length;

            if(difference != 0)
            {
                T[] newInstanceArray = new T[instanceCount];

                // copy existing
                for(int i = 0;
                    i < instanceArray.Length && i < instanceCount;
                    ++i)
                {
                    newInstanceArray[i] = instanceArray[i];
                }

                // create new
                for(int i = instanceArray.Length;
                    i < instanceCount;
                    ++i)
                {
                    GameObject displayGO = GameObject.Instantiate(template.gameObject);
                    displayGO.name = instanceName + " [" + i.ToString("00") + "]";
                    displayGO.transform.SetParent(container, false);
                    displayGO.SetActive(true);

                    newInstanceArray[i] = displayGO.GetComponent<T>();
                }

                // destroy excess
                for(int i = instanceCount;
                    i < instanceArray.Length;
                    ++i)
                {
                    GameObject.Destroy(instanceArray[i].gameObject);
                }

                // assign
                instanceArray = newInstanceArray;
            }

            // reactivate
            if(reactivateAll)
            {
                foreach(T instance in instanceArray)
                {
                    instance.gameObject.SetActive(false);
                    instance.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>Comparer for the OrderByNext function described below.</summary>
        private class OrderByNextComparer : IComparer<float>
        {
            /// <summary>Comparison function allows for duplicate values in the SortedList.</summary>
            public int Compare(float x, float y)
            {
                int result = x.CompareTo(y);
                if(result == 0)
                {
                    return 1;
                }
                else
                {
                    return result;
                }
            }
        }

        /// <summary>Sorts the given list by "nextness" from the origin behaviour in the given direction.</summary>
        /// <remarks>Modified from the [UnityEngine.UI.Selectable](https://bitbucket.org/Unity-Technologies/ui/src/2019.1/UnityEngine.UI/UI/Core/Selectable.cs)
        /// class.</remarks>
        public static IList<T> OrderByNext<T>(RectTransform origin, List<T> behaviourList, Vector3 dir)
        where T : UnityEngine.EventSystems.UIBehaviour
        {
            // asserts
            Debug.Assert(origin != null);
            Debug.Assert(behaviourList != null);

            if(behaviourList.Count == 0 || dir == Vector3.zero) { return new List<T>(0); }

            // setup
            dir = dir.normalized;

            Vector3 pos = Vector3.zero;
            SortedList<float,T> sortedList = new SortedList<float,T>(new OrderByNextComparer());

            // Set pos to edge of rect
            RectTransform rectTransform = origin.transform as RectTransform;
            if(rectTransform != null)
            {
                Vector2 localDir = Quaternion.Inverse(origin.transform.rotation) * dir;

                if (localDir != Vector2.zero)
                {
                    localDir /= Mathf.Max(Mathf.Abs(localDir.x), Mathf.Abs(localDir.y));
                }

                pos = rectTransform.rect.center + Vector2.Scale(rectTransform.rect.size, localDir * 0.5f);
            }
            pos = origin.transform.TransformPoint(pos);

            // create list
            foreach(T item in behaviourList)
            {
                if(item == null || item.transform == origin) { continue; }

                var itemRect = item.transform as RectTransform;
                Vector3 itemCenter = itemRect != null ? (Vector3)itemRect.rect.center : Vector3.zero;
                Vector3 myVector = item.transform.TransformPoint(itemCenter) - pos;

                // Value that is the distance out along the direction.
                float dot = Vector3.Dot(dir, myVector);

                // Skip elements that are in the wrong direction or which have zero distance.
                // This also ensures that the scoring formula below will not have a division by zero error.
                if (dot <= 0)
                    continue;

                // This scoring function has two priorities:
                // - Score higher for positions that are closer.
                // - Score higher for positions that are located in the right direction.
                // This scoring function combines both of these criteria.
                // It can be seen as this:
                //   Dot (dir, myVector.normalized) / myVector.magnitude
                // The first part equals 1 if the direction of myVector is the same as dir, and 0 if it's orthogonal.
                // The second part scores lower the greater the distance is by dividing by the distance.
                // The formula below is equivalent but more optimized.
                //
                // If a given score is chosen, the positions that evaluate to that score will form a circle
                // that touches pos and whose center is located along dir. A way to visualize the resulting functionality is this:
                // From the position pos, blow up a circular balloon so it grows in the direction of dir.
                // The first Selectable whose center the circular balloon touches is the one that's chosen.
                float score = dot / myVector.sqrMagnitude;

                sortedList.Add(score, item);
            }

            return sortedList.Values;
        }

        // ---------[ OBSOLETE ]---------
        /// <summary>[Obsolete] Finds the first instance of a component in the active scene.</summary>
        [Obsolete("Use UIUtilities.FindComponentInAllScenes() instead.")]
        public static T FindComponentInScene<T>(bool includeInactive)
        where T : class
        {
            /*
             * JC (2019-09-07): UIs are sometimes managed in their own scenes
             * (e.g. one scene per UI panel/screen), and those scenes will usually
             * not be the active scenes. For the purpose of resolving a singleton
             * instance, Resources.FindObjectsOfTypeAll<T>() is probably the safer
             * approach (Object.FindObjectOfType(type) would be more efficient but
             * cannot return inactive objects.
             */
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            IEnumerable<GameObject> rootObjects = activeScene.GetRootGameObjects();
            T foundComponent = null;

            foreach(var root in rootObjects)
            {
                if(includeInactive
                   || root.activeInHierarchy)
                {
                    foundComponent = root.GetComponent<T>();
                    if(foundComponent != null)
                    {
                        return foundComponent;
                    }

                    foundComponent = root.GetComponentInChildren<T>(includeInactive);
                    if(foundComponent != null)
                    {
                        return foundComponent;
                    }
                }
            }

            return null;
        }

        /// <summary>[Obsolete] Finds components within the active scene.</summary>
        [Obsolete("Use UIUtilities.FindComponentsInLoadedScenes() instead.")]
        public static List<T> FindComponentsInScene<T>(bool includeInactive)
        where T : class
        {
            // JC (2019-09-07): See comment above (FindComponentInScene).
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            IEnumerable<GameObject> rootObjects = activeScene.GetRootGameObjects();
            List<T> retVal = new List<T>();

            foreach(var root in rootObjects)
            {
                if(includeInactive
                   || root.activeInHierarchy)
                {
                    retVal.AddRange(root.GetComponents<T>());
                    retVal.AddRange(root.GetComponentsInChildren<T>(includeInactive));
                }
            }

            return retVal;
        }
    }
}
