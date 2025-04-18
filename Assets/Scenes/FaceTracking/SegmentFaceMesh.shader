Shader "Custom/SegmentFaceMesh"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1) // Base color (including its alpha for overall opacity)
        _FillAmount ("Fill Amount", Range(0.0, 1.0)) = 0.5 // Our control parameter
        _FillDirection ("Fill Direction", Range(0.0, 1.0)) = 0.5
        _BaseAlpha ("Base Alpha", Range(0.0, 1.0)) = 0.25
        _SubmeshUVMinY ("Submesh UV Min Y", Float) = 0.0
        _SubmeshUVMaxY ("Submesh UV Max Y", Float) = 1.0
        _SubmeshUVMinX ("Submesh UV Min X", Float) = 0.0
        _SubmeshUVMaxX ("Submesh UV Max X", Float) = 1.0
        // Optional: Add smoothness for the transition edge
        // _Smoothness ("Smoothness", Range(0.01, 1.0)) = 0.05
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        ZWrite Off // Don't write to depth buffer for transparent objects
        Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            float _FillAmount;
            float _BaseAlpha;
            float _SubmeshUVMinY;
            float _SubmeshUVMaxY;
            float _SubmeshUVMinX;
            float _SubmeshUVMaxX;
            float _FillDirection;
            // float _Smoothness; // Uncomment if using smoothness

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // Pass UV coordinates to the fragment shader
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Get base color and target alpha from the material's _Color property
                fixed4 col = _Color;
                float targetAlpha = _Color.a; // The alpha value we want when fully filled (e.g., 1.0)

                // --- Calculate Normalized UV ---
                // Avoid division by zero if min == max
                float uvRangeY = max(0.0001, _SubmeshUVMaxY - _SubmeshUVMinY);
                // Normalize current pixel's uv.y within the submesh's specific 0-1 range
                float normalizedY = saturate((i.uv.y - _SubmeshUVMinY) / uvRangeY);
                float uvRangeX = max(0.0001, _SubmeshUVMaxX - _SubmeshUVMinX);
                // Normalize current pixel's uv.y within the submesh's specific 0-1 range
                float normalizedX = saturate((i.uv.x - _SubmeshUVMinX) / uvRangeX);
                
                // --- Calculate Fill Progression (0.0 to 1.0) ---
                // Use the same logic as before (step or smoothstep) based on UVs and _FillAmount
                // This determines *how filled* this specific pixel is (0 = not filled, 1 = fully filled)
            
                // Hard edge:
                // float fillProgression = step(i.uv.y, _FillAmount); // 0 if unfilled, 1 if filled (adjust UVs if needed)
                float verticalProgression = step(normalizedY, _FillAmount); // Example: bottom-up
                float horizontalProgression = step(normalizedX, _FillAmount); // Example: left-to-right

                // float fillProgression = step(normalizedY, _FillAmount); // 0 if unfilled, 1 if filled (adjust UVs if needed)
                float fillProgression = lerp(verticalProgression, horizontalProgression, _FillDirection); // vertical if 0, horizontal if 1
            
                // Optional: Smooth edge (uncomment if using _Smoothness property):
                // float edgeMin = _FillAmount - _Smoothness * 0.5;
                // float edgeMax = _FillAmount + _Smoothness * 0.5;
                // float fillProgression = smoothstep(edgeMin, edgeMax, i.uv.y); // 0 to 1 smooth transition
            
                // --- Calculate Final Alpha ---
                // Interpolate between _BaseAlpha and targetAlpha based on the fillProgression
                // lerp(a, b, t) = linearly interpolates between a and b using t
                float finalAlpha = lerp(_BaseAlpha, targetAlpha, fillProgression);
            
                // --- Apply final color and alpha ---
                col.rgb = _Color.rgb; // Use the RGB from the material's color
                col.a = finalAlpha;   // Set the calculated alpha
            
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}