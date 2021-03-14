/*
MIT License

Copyright (c) 2021 Haï~ (@vr_hai github.com/hai-vr)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ChaperoneTweaker
{
   [CustomEditor(typeof(ChaperoneTweaker))]
   [CanEditMultipleObjects]
   public class ChaperoneTweakerEditor : Editor
   {
      public SerializedProperty text;
      public SerializedProperty loaded;
      public SerializedProperty loadedText;
      public SerializedProperty universeCount;
      public SerializedProperty selectedUniverse;
      public SerializedProperty readyForEditing;

      private void OnEnable()
      {
         text = serializedObject.FindProperty("text");
         loaded = serializedObject.FindProperty("loaded");
         loadedText = serializedObject.FindProperty("loadedText");
         universeCount = serializedObject.FindProperty("universeCount");
         selectedUniverse = serializedObject.FindProperty("selectedUniverse");
         readyForEditing = serializedObject.FindProperty("readyForEditing");
      }

      public override void OnInspectorGUI()
      {
         serializedObject.Update();

         if (!loaded.boolValue)
         {
            LayoutNotYetLoaded();
         }
         else if (!readyForEditing.boolValue)
         {
            LayoutSelectUniverse();
         }
         else
         {
            LayoutWorking();
         }

         serializedObject.ApplyModifiedProperties();
      }

      private void LayoutNotYetLoaded()
      {
         UiAsset();

         EditorGUI.BeginDisabledGroup(text.objectReferenceValue == null);
         if (GUILayout.Button("Load"))
         {
            Load();
         }
         EditorGUI.EndDisabledGroup();
      }

      private void LayoutSelectUniverse()
      {
         EditorGUI.BeginDisabledGroup(true);
         UiAsset();
         EditorGUI.EndDisabledGroup();

         UiUniverseSelector();

         EditorGUI.BeginDisabledGroup(selectedUniverse.intValue < 0);
         if (GUILayout.Button("Confirm universe selection"))
         {
            ReadyUp();
         }

         EditorGUI.EndDisabledGroup();
      }

      private void LayoutWorking()
      {
         EditorGUI.BeginDisabledGroup(true);
         UiAsset();
         EditorGUI.EndDisabledGroup();
         UpdateLineRenderers();
         if (GUILayout.Button("Overwrite asset with new positions"))
         {
            OverwriteFileWithNewPositions();
         }
      }

      private void UiAsset()
      {
         EditorGUILayout.PropertyField(text, new GUIContent("Steam/chaperone.vrchap File"));
      }

      private void UiUniverseSelector()
      {
         var options = Enumerable.Range(-1, universeCount.intValue + 1)
            .Select(i => i == -1 ? "Select an universe..." : i.ToString())
            .ToArray();
         var previousValue = selectedUniverse.intValue;
         selectedUniverse.intValue = EditorGUILayout.Popup("Universe", previousValue + 1, options) - 1;

         if (selectedUniverse.intValue != previousValue)
         {
            UpdateLineRenderers();
         }
      }

      private void Load()
      {
         if (loaded.boolValue) return;

         var currentText = ((TextAsset) text.objectReferenceValue).text;
         var node = ChapJSON.Parse(currentText);

         var currentUniverseCount = node.AsObject["universes"].Count;
         if (currentUniverseCount == 0) return;

         var hasOnlyOneUniverse = currentUniverseCount == 1;
         universeCount.intValue = currentUniverseCount;
         loadedText.stringValue = currentText;
         loaded.boolValue = true;
         selectedUniverse.intValue = hasOnlyOneUniverse ? 0 : -1;
         readyForEditing.boolValue = hasOnlyOneUniverse;

         if (hasOnlyOneUniverse)
         {
            ReadyUp();
         }
      }

      private void ReadyUp()
      {
         if (selectedUniverse.intValue < 0) return;

         var simplifiedVectors = ReadUniverseAsSimplifiedVectors(selectedUniverse.intValue);
         var thatTransform = ((ChaperoneTweaker) target).transform;

         for (var i = 0; i < simplifiedVectors.Count; i++)
         {
            var point = simplifiedVectors[i];
            var current = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            current.name = "_" + i;
            current.transform.parent = thatTransform;
            current.transform.localPosition = point;
            current.transform.localScale = Vector3.one * 0.1f;
         }

         readyForEditing.boolValue = true;
      }

      private void OverwriteFileWithNewPositions()
      {
         var original = ChapJSON.Parse(loadedText.stringValue);

         var simplifiedVectors = ReadChildTransformsAsSimplifiedVectors();
         var complexVectors = ConvertSimplifiedVectorsToComplexVectors(simplifiedVectors);
         var wallsJson = ConvertComplexVectorsToJson(complexVectors);

         original.AsObject["universes"].AsArray[selectedUniverse.intValue].AsObject["collision_bounds"] = wallsJson;

         var jsonString = new StringBuilder();
         original.WriteToStringBuilder(jsonString, 0, 2, JSONTextMode.Indent);

         var writer = new StreamWriter(AssetDatabase.GetAssetPath((TextAsset) text.objectReferenceValue), false);
         writer.WriteLine(jsonString.ToString());
         writer.Close();
      }

      private static JSONArray ConvertComplexVectorsToJson(Vector3[][] complexVectors)
      {
         var walls = complexVectors.Select(rectangularWallRepresentation =>
         {
            var rectangularWallRepresentationJson = new JSONArray();
            foreach (var vector in rectangularWallRepresentation)
            {
               var xyzCoordinatesJson = VectorToJsonArray(vector);
               rectangularWallRepresentationJson.Add(xyzCoordinatesJson);
            }

            return rectangularWallRepresentationJson;
         });

         var wallsJson = new JSONArray();
         foreach (var wall in walls)
         {
            wallsJson.Add(wall);
         }

         return wallsJson;
      }

      private static JSONArray VectorToJsonArray(Vector3 vector)
      {
         var xyzCoordinatesJson = new JSONArray();
         xyzCoordinatesJson.Add(vector.x);
         xyzCoordinatesJson.Add(vector.y);
         xyzCoordinatesJson.Add(vector.z);
         return xyzCoordinatesJson;
      }

      private static Vector3[][] ConvertSimplifiedVectorsToComplexVectors(List<Vector3> simplifiedVectors)
      {
         // No idea what this number is, it was in my chaperone file.
         // SteamVR will overwrite this value anyways next time SteamVR closes.
         const float someMagicNumber = 2.4300000667572021f;

         return simplifiedVectors.Select((current, i) =>
         {
            var next = simplifiedVectors[(i + 1) % simplifiedVectors.Count];
            return new[]
            {
               new Vector3(current.x, 0f, current.z),
               new Vector3(current.x, someMagicNumber, current.z),
               new Vector3(next.x, someMagicNumber, next.z),
               new Vector3(next.x, 0f, next.z)
            };
         }).ToArray();
      }

      private List<Vector3> ReadChildTransformsAsSimplifiedVectors()
      {
         var that = ((ChaperoneTweaker) target).transform;
         return that.Cast<Transform>()
            .SelectMany(OnlyLeavesRecursive)
            .Select(transform => that.InverseTransformPoint(transform.position))
            .ToList();
      }

      private static IEnumerable<Transform> OnlyLeavesRecursive(Transform transform)
      {
         return transform.childCount == 0
            ? new[] {transform}
            : transform.transform.Cast<Transform>().SelectMany(OnlyLeavesRecursive);
      }

      private List<Vector3> ReadUniverseAsSimplifiedVectors(int universeIndex)
      {
         var node = ChapJSON.Parse(loadedText.stringValue);
         var collisionBounds = node.AsObject["universes"].AsArray[universeIndex].AsObject["collision_bounds"].AsArray;
         var intermediateBounds = ConvertJsonToArrayStructure(collisionBounds);
         var simplifiedVectors = ConvertArrayStructureToSimplifiedVectors(intermediateBounds);
         return simplifiedVectors;
      }

      private static double[][][] ConvertJsonToArrayStructure(JSONArray collisionBounds)
      {
         return collisionBounds.Children.Select(jsonNode => jsonNode.AsArray)
            .Select(rectangularWallRepresentation => rectangularWallRepresentation.Children.Select(jsonNode => jsonNode.AsArray)
               .Select(xyzCoordinates => xyzCoordinates.Children.Select(jsonNode => jsonNode.AsDouble).ToArray())
               .ToArray())
            .ToArray();
      }

      private List<Vector3> ConvertArrayStructureToSimplifiedVectors(double[][][] universe)
      {
         return universe.Select(line => new Vector3((float) line[0][0], 0, (float) line[0][2])).ToList();
      }

      private void UpdateLineRenderers()
      {
         if (readyForEditing.boolValue)
         {
            CreateOrOverwriteLineRendererPositions(ReadChildTransformsAsSimplifiedVectors());
         }
         else
         {
            if (selectedUniverse.intValue < 0)
            {
               var lineRenderer = GetOrCreateLineRenderer();
               lineRenderer.positionCount = 0;
            }
            else
            {
               CreateOrOverwriteLineRendererPositions(ReadUniverseAsSimplifiedVectors(selectedUniverse.intValue));
            }
         }
      }

      private void CreateOrOverwriteLineRendererPositions(List<Vector3> simplifiedVectors)
      {
         var lineRenderer = GetOrCreateLineRenderer();

         lineRenderer.positionCount = simplifiedVectors.Count + 1;
         for (var index = 0; index < simplifiedVectors.Count; index++)
         {
            var simplifiedVector = simplifiedVectors[index];
            lineRenderer.SetPosition(index, simplifiedVector);
         }
         lineRenderer.SetPosition(lineRenderer.positionCount - 1, simplifiedVectors[0]);
      }

      private LineRenderer GetOrCreateLineRenderer()
      {
         var chapFix = (ChaperoneTweaker)serializedObject.targetObject;
         var lineRenderer = chapFix.GetComponent<LineRenderer>();
         if (lineRenderer != null) return lineRenderer;

         lineRenderer = chapFix.gameObject.AddComponent<LineRenderer>();
         lineRenderer.widthMultiplier = 0.02f;
         lineRenderer.useWorldSpace = false;
         InternalEditorUtility.SetIsInspectorExpanded(lineRenderer, false);

         return lineRenderer;
      }
   }
}
