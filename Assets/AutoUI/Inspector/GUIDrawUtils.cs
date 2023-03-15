using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AutoUI.Inspector {
public class GUIDrawUtils {
	
	// cached assets
	private static Texture2D SquigglyLineTexture;
	private static readonly Dictionary<int, Texture2D> CircleTextures = new(); // <radius, texture> all of them are white
	
	public static void DrawSquigglyLine(int startPos, int endPos, CodeAreaState state) {
		GUI.color = Color.white;

		Texture2D squigglyLineTexture = GetSquigglyLineTexture();

		int lineStart = 0;
		for (int i = 0; i < state.Lines.Length; i++) {
			int lineLength = state.LineLengths[i];
			if (startPos < lineStart + lineLength) {
				int end = Math.Min(endPos, lineStart + lineLength);
				Rect position = new((startPos - lineStart) * state.CharWidth, (i + 1) * state.LineHeight - squigglyLineTexture.height, (end - startPos) * state.CharWidth, squigglyLineTexture.height);
				GUI.DrawTextureWithTexCoords(position, squigglyLineTexture, new Rect(0, 0, position.width / squigglyLineTexture.width, 1));
				startPos = end + 1; // +1 for the newline character
				if (endPos == end) break;
			}

			lineStart += lineLength + 1;
		}
	}

	public static void DrawRoundRect(Rect rect, Color color, float width, float radius) {
		// Draw the four sides
		DrawLine(new Vector2(rect.x + radius, rect.y), new Vector2(rect.x + rect.width - radius, rect.y), color, width);
		DrawLine(new Vector2(rect.x + rect.width, rect.y + radius), new Vector2(rect.x + rect.width, rect.y + rect.height - radius), color, width);
		DrawLine(new Vector2(rect.x + rect.width - radius, rect.y + rect.height), new Vector2(rect.x + radius, rect.y + rect.height), color, width);
		DrawLine(new Vector2(rect.x, rect.y + rect.height - radius), new Vector2(rect.x, rect.y + radius), color, width);

		if (radius <= 0) return;

		// Draw the corners
		DrawArc(new Vector2(rect.x + radius, rect.y + radius), radius, 180f, 270f, color, width);
		DrawArc(new Vector2(rect.x + rect.width - radius, rect.y + radius), radius, 270f, 360f, color, width);
		DrawArc(new Vector2(rect.x + rect.width - radius, rect.y + rect.height - radius), radius, 0f, 90f, color, width);
		DrawArc(new Vector2(rect.x + radius, rect.y + rect.height - radius), radius, 90f, 180f, color, width);
	}

	public static void FillRoundRect(Rect rect, Color color, float radius) {
		Color oldColor = GUI.color;
		GUI.color = color;

		if (radius > 0) {
			Texture2D circle = GetCircleTexture((int)radius);

			// Draw the corners by just drawing different parts of the same circle texture (texture coords are from bottom to top, while UI coords are from top to bottom)
			GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, radius, radius), circle, new Rect(0, 0.5f, 0.5f, 0.5f));
			GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - radius, rect.y, radius, radius), circle, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
			GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.yMax - radius, radius, radius), circle, new Rect(0, 0, 0.5f, 0.5f));
			GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - radius, rect.yMax - radius, radius, radius), circle, new Rect(0.5f, 0, 0.5f, 0.5f));

			// Draw the sides (the -1 and +2 are to fix line gaps, probably caused by floating point inaccuracies)
			GUI.DrawTexture(new Rect(rect.x + radius - 1, rect.y, rect.width + 2 - 2 * radius, radius), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.x + radius - 1, rect.yMax - radius, rect.width + 2 - 2 * radius, radius), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.x, rect.y + radius, radius, rect.height - 2 * radius), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.xMax - radius, rect.y + radius, radius, rect.height - 2 * radius), EditorGUIUtility.whiteTexture);
		}

		// Draw the center
		GUI.DrawTexture(new Rect(rect.x + radius - 1, rect.y + radius, rect.width + 2 - 2 * radius, rect.height - 2 * radius), EditorGUIUtility.whiteTexture);

		GUI.color = oldColor;
	}

	private static Texture2D GetCircleTexture(int radius) {
		if (CircleTextures.ContainsKey(radius) && CircleTextures[radius] != null) return CircleTextures[radius];

		int diameter = 2 * radius;
		Vector2 center = new(radius, radius);
		Texture2D texture = new(diameter, diameter);
		for (int x = 0; x < diameter; x++)
		for (int y = 0; y < diameter; y++) {
			float distance = Vector2.Distance(new Vector2(x, y), center);
			texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
		}

		texture.Apply();
		CircleTextures[radius] = texture;
		return texture;
	}

	private static Texture2D GetSquigglyLineTexture() {
		if (SquigglyLineTexture != null) return SquigglyLineTexture;


		SquigglyLineTexture = new Texture2D(4, 3);
		SquigglyLineTexture.SetPixels(new[] {
			Color.clear, Color.red, Color.clear, Color.clear,
			Color.red, new(0.85882352941f, 0, 0, 0.25f), Color.red, Color.clear,
			Color.clear, Color.clear, Color.clear, Color.red
		});
		SquigglyLineTexture.Apply(false);
		SquigglyLineTexture.wrapMode = TextureWrapMode.Repeat;
		return SquigglyLineTexture;
	}

	private static void DrawLine(Vector2 start, Vector2 end, Color color, float width) {
		Handles.BeginGUI();
		Handles.color = color;
		Handles.DrawAAPolyLine(width, start, end);
		Handles.EndGUI();
	}

	private static void DrawArc(Vector2 center, float radius, float startAngle, float endAngle, Color color, float width) {
		Handles.BeginGUI();
		Handles.color = color;
		Vector3 from = new(Mathf.Cos(Mathf.Deg2Rad * startAngle), Mathf.Sin(Mathf.Deg2Rad * startAngle), 0f);
		Vector3 to = new(Mathf.Cos(Mathf.Deg2Rad * endAngle), Mathf.Sin(Mathf.Deg2Rad * endAngle), 0f);
		Handles.DrawWireArc(center, Vector3.forward, from, endAngle - startAngle, radius);
		Handles.EndGUI();
	}
}
}