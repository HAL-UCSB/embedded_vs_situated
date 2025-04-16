// Shader "Custom/UnlitTransparentFill"
Shader "Custom/SegmentFaceMesh"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1) // Base color (including its alpha for overall opacity)
        _FillAmount ("Fill Amount", Range(0.0, 1.0)) = 0.5 // Our control parameter
        _BaseAlpha ("Base Alpha", Range(0.0, 1.0)) = 0.25
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
            // float _Smoothness; // Uncomment if using smoothness

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // Pass UV coordinates to the fragment shader
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            // fixed4 frag (v2f i) : SV_Target
            // {
            //     fixed4 col = _Color;

            //     // --- Fill Logic ---
            //     // Compare the vertical UV coordinate (i.uv.y) with the fill amount
            //     // Note: If V=0 is top and V=1 is bottom, you might need '1.0 - i.uv.y'
            //     //       or reverse the comparison logic. Test to see what works.

            //     // Hard edge:
            //     float fillAlpha = step(i.uv.y, _FillAmount); // alpha is 1 if uv.y <= _FillAmount, else 0

            //     // --- Optional: Smooth edge ---
            //     // float edgeMin = _FillAmount - _Smoothness * 0.5;
            //     // float edgeMax = _FillAmount + _Smoothness * 0.5;
            //     // float fillAlpha = smoothstep(edgeMin, edgeMax, i.uv.y); // Smooth transition based on uv.y

            //     // --- Apply calculated alpha ---
            //     // Modulate base color alpha by the fillAlpha
            //     col.a *= fillAlpha;

            //     // apply fog
            //     UNITY_APPLY_FOG(i.fogCoord, col);
            //     return col;
            // }
            fixed4 frag (v2f i) : SV_Target
            {
                // Get base color and target alpha from the material's _Color property
                fixed4 col = _Color;
                float targetAlpha = _Color.a; // The alpha value we want when fully filled (e.g., 1.0)
            
                // --- Calculate Fill Progression (0.0 to 1.0) ---
                // Use the same logic as before (step or smoothstep) based on UVs and _FillAmount
                // This determines *how filled* this specific pixel is (0 = not filled, 1 = fully filled)
            
                // Hard edge:
                float fillProgression = step(i.uv.y, _FillAmount); // 0 if unfilled, 1 if filled (adjust UVs if needed)
            
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