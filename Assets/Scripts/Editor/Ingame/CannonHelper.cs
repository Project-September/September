# if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;


namespace InGame.Exhibit.Editor
{
	[CustomEditor(typeof(CannonInteractable))]
	public class CannonHelper : UnityEditor.Editor
	{
		private SerializedProperty _angleLimitX;
		private SerializedProperty _angleLimitY;
		private SerializedProperty _barrel;

		private void OnEnable()
		{
			_angleLimitX = serializedObject.FindProperty("_rotateAngleLimitX");
			_angleLimitY = serializedObject.FindProperty("_rotateAngleLimitY");
			 _barrel = serializedObject.FindProperty("_cannonBarrel");
			 if (_barrel == null)
			 {
				 Debug.LogError("CannonHelper: _cannonBarrelが見つかりませんでした。");
			 }
		}

		private void OnSceneGUI()
		{
			var limit = (CannonInteractable)target;
			var t = limit.transform;
			var pos = t.position;
			var barrelPos = _barrel.objectReferenceValue != null ? ((Transform)_barrel.objectReferenceValue).position : pos;
			var radius = limit.transform.localScale.y;

			var forward = t.forward;
			var maxX = _angleLimitX.vector2Value.y;
			var minX = _angleLimitX.vector2Value.x;
			var maxY = _angleLimitY.vector2Value.y;
			var minY = _angleLimitY.vector2Value.x;

			// ドラッグ可能な点の作成
			DrawAngleHandle(
				ref maxX,
				Color.red,
				pos,
				t.up,
				forward,
				radius
			);
			DrawAngleHandle(
				ref minX,
				Color.red,
				pos,
				t.up,
				forward,
				radius
			);
			DrawAngleHandle(
				ref maxY,
				Color.blue,
				barrelPos,
				t.right,
				forward,
				radius
			);
			DrawAngleHandle(
				ref minY,
				Color.blue,
				barrelPos,
				t.right,
				forward,
				radius
			);

			// inspector上の値更新
			_angleLimitX.vector2Value = new Vector2(minX, maxX);
			_angleLimitY.vector2Value = new Vector2(minY, maxY);
			serializedObject.ApplyModifiedProperties();

			// 可動範囲の描画
			CreateLimitAngleView(pos, t.up, forward, radius, minX, maxX,
				Color.yellow);
			CreateLimitAngleView(barrelPos, t.right, forward, radius, minY, maxY,
				Color.cyan);
		}

		private void CreateLimitAngleView(Vector3 center, Vector3 axis, Vector3 forward, float radius, float minAngle,
			float maxAngle, Color color)
		{
			// 最小角
			var minRot = Quaternion.AngleAxis(minAngle, axis);
			var minDir = minRot * forward;
			// 最大角
			var maxRot = Quaternion.AngleAxis(maxAngle, axis);
			var maxDir = maxRot * forward;

			// 円形の描画
			Handles.color = color;
			Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
			Handles.DrawSolidArc(
				center,
				axis,
				minDir,
				maxAngle - minAngle,
				radius
			);
			Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
			Handles.DrawWireArc(
				center,
				axis,
				minDir,
				360f,
				radius
			);
			Handles.DrawLine(center, center + minDir * radius);
			Handles.DrawLine(center, center + maxDir * radius);
		}

		private void DrawAngleHandle(
			ref float angle,
			Color color,
			Vector3 pos,
			Vector3 axis,
			Vector3 forward,
			float radius)
		{

			var rot = Quaternion.AngleAxis(angle, axis);
			var dir = rot * forward;

			var handlePos = pos + dir * radius;

			// ハンドルの描画
			EditorGUI.BeginChangeCheck();

			Handles.color = color;
			var newPos = Handles.FreeMoveHandle(
				handlePos,
				0.1f,
				Vector3.zero,
				Handles.SphereHandleCap
			);

			if (EditorGUI.EndChangeCheck())
			{
				var newDir = (newPos - pos).normalized;

				angle = Vector3.SignedAngle(
					forward,
					newDir,
					axis
				);
			}

			SceneView.RepaintAll();
		}
	}
}
#endif