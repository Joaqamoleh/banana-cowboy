// simple normal blending
void alphaBlend_float(float4 top, float4 bottom, out float4 ret) {
    float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
    float alpha = top.a + bottom.a * (1 - top.a);

    ret = float4(color, alpha);
}