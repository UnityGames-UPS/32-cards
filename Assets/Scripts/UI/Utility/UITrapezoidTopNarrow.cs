using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UITrapezoidTopNarrow : BaseMeshEffect
{
  [Tooltip("Positive = top edge shrinks inward. Units are UI local units (pixels-ish).")]
  [Range(-300f, 300f)] public float TopInset = 60f;

  [Tooltip("Positive = bottom edge shrinks inward. Negative = bottom edge expands outward.")]
  [Range(-300f, 300f)] public float BottomInset = 0f;

  public override void ModifyMesh(VertexHelper vh)
  {
    if (!IsActive() || vh.currentVertCount == 0) return;

    UIVertex v = new UIVertex();

    // Find bounds of the generated quad
    Vector2 p0 = Vector2.zero;
    vh.PopulateUIVertex(ref v, 0);
    p0 = v.position;

    float minX = p0.x, maxX = p0.x, minY = p0.y, maxY = p0.y;

    for (int i = 1; i < vh.currentVertCount; i++)
    {
      vh.PopulateUIVertex(ref v, i);
      var p = (Vector2)v.position;
      if (p.x < minX) minX = p.x;
      if (p.x > maxX) maxX = p.x;
      if (p.y < minY) minY = p.y;
      if (p.y > maxY) maxY = p.y;
    }

    const float eps = 0.001f;

    for (int i = 0; i < vh.currentVertCount; i++)
    {
      vh.PopulateUIVertex(ref v, i);
      var p = v.position;

      bool isTop = Mathf.Abs(p.y - maxY) < eps;
      bool isBottom = Mathf.Abs(p.y - minY) < eps;

      if (isTop)
      {
        // move left top rightwards, right top leftwards
        if (Mathf.Abs(p.x - minX) < eps) p.x += TopInset;
        if (Mathf.Abs(p.x - maxX) < eps) p.x -= TopInset;
      }

      if (isBottom)
      {
        if (Mathf.Abs(p.x - minX) < eps) p.x += BottomInset;
        if (Mathf.Abs(p.x - maxX) < eps) p.x -= BottomInset;
      }

      v.position = p;
      vh.SetUIVertex(v, i);
    }
  }

#if UNITY_EDITOR
  protected override void OnValidate()
  {
    base.OnValidate();
    if (graphic) graphic.SetVerticesDirty();
  }
#endif
}
