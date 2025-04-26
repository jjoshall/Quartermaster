using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Written following below tutorial:
// LineRenderer Tutorial - https://www.youtube.com/watch?v=--LB7URk60A

public class UILineRenderer : Graphic
{
    public Vector2Int gridSize;

    public List<Vector2> points;
    float width;
    float height; 
    float unitWidth;
    float unitHeight;

    public float thickness = 10f;

    protected override void OnPopulateMesh (VertexHelper vh){
        Draw(vh);
    }


    void Draw(VertexHelper vh)
    {
        vh.Clear();

        width = rectTransform.rect.width;
        height = rectTransform.rect.height;

        unitWidth = width / (float)gridSize.x;
        unitHeight = height / (float)gridSize.y;

        if (points.Count < 2) return; // not enough points to draw a line.

        for (int i = 0; i < points.Count; i++){
            Vector2 point = points[i];
            DrawVerticesForPoint(point, vh);

        }
        for (int i = 0; i < points.Count - 1; i++){
            int index = i * 2;
            vh.AddTriangle(index, index + 1, index + 3);
            vh.AddTriangle(index + 3, index + 2, index + 0);
        }
    }

    void DrawVerticesForPoint(Vector2 point, VertexHelper vh){
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = new Vector3(-thickness / 2, 0);
        vertex.position += new Vector3 (unitWidth * point.x, unitHeight * point.y, 0f);
        vh.AddVert(vertex);

        vertex.position = new Vector3(thickness / 2, 0);
        vertex.position += new Vector3 (unitWidth * point.x, unitHeight * point.y, 0f);
        vh.AddVert(vertex);
    }

    
}
