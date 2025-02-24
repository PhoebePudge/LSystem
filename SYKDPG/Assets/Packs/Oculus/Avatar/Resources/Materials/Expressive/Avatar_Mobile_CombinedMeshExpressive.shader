//
// OvrAvatar Mobile combined mesh expressive shader
// For use on expressive face meshes
// Texture array approach for rendering a combined mesh avatar with blend shape expression
// Coupled with OvrAvatarMaterialManager to populate the texture arrays
//
// Unity vertex-fragnment implementation
// Simplified lighting model recommended for use on mobile supporting one directional light
// Surface shader recommended on PC
//
// Uses transparent queue for fade effects
//
// Color and appearance of the facial regions controlled via MeshObject&B channels in roughness texture
// Pupil size controlled by manipulating UV coordinates
//
// Shader keywords:
// - SECONDARY_LIGHT_ON SECONDARY_LIGHT_OFF
//   Enable SECONDARY_LIGHT_ON for a second "light" comprised of _SecondaryLightDirection and
//   _SecondaryLightColor This will influence the rim effect providing a lit contour to the avatar
//


Shader "OvrAvatar/Avatar_Mobile_CombinedMeshExpressive"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Main Texture Array", 2DArray) = "white" {}
        [NoScaleOffset] _NormalMap("Normal Map Array", 2DArray) = "bump" {}
        [NoScaleOffset] _RoughnessMap("Roughness Map Array", 2DArray) = "black" {}
        
        _Dimmer("Dimmer", Range(0.0,1.0)) = 1.0
        _Alpha("Alpha", Range(0.0,1.0)) = 1.0

        // Index into the texture array needs an offset for precision
        _Slices("Texture Array Slices", int) = 4.97

        _PupilSize("Pupil Size", Range(-1, 2)) = 0
        _LipSmoothness("Lip Smoothness", Range(0, 1)) = 0

        _MaskColorIris("Iris Color", Color) = (0.0,0.0,0.0,1.0)
        _MaskColorLips("Lips Color", Color) = (0.0,0.0,0.0,1.0)
        _MaskColorBrows("Brows Color", Color) = (0.0,0.0,0.0,1.0)
        _MaskColorLashes("Lashes Color", Color) = (0.0,0.0,0.0,1.0)
        _MaskColorSclera("Sclera Color", Color) = (0.0,0.0,0.0,1.0)
        _MaskColorGums("Gums Color", Color) = (0.0,0.0,0.0,1.0)
        _MaskColorTeeth("Teeth Color", Color) = (0.0,0.0,0.0,1.0)

        [HideInInspector] _SrcBlend("", Float) = 1
        [HideInInspector] _DstBlend("", Float) = 0
    }

    SubShader
    {
        Tags { "LightMode" = "ForwardBase" "IgnoreProjector" = "True"}
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile SECONDARY_LIGHT_OFF SECONDARY_LIGHT_ON
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            UNITY_DECLARE_TEX2DARRAY(_MainTex);
            UNITY_DECLARE_TEX2DARRAY(_NormalMap);
            float4 _NormalMap_ST;
            UNITY_DECLARE_TEX2DARRAY(_RoughnessMap);

            int _Slices;

            half _Dimmer;
            half _Alpha;

            half4 _BaseColor[5];
            half _DiffuseIntensity[5];
            half _RimIntensity[5];
            half _ReflectionIntensity[5];

            half3 _SecondaryLightDirection;
            half4 _SecondaryLightColor;

            half _PupilSize;
            half _LipSmoothness;

            fixed4 _MaskColorIris;
            fixed4 _MaskColorSclera;
            fixed4 _MaskColorBrows;
            fixed4 _MaskColorLashes;
            fixed4 _MaskColorLashesEnd;
            fixed4 _MaskColorLips;
            fixed4 _MaskColorGums;
            fixed4 _MaskColorTeeth;

            static const int ONE = 1;
            static const fixed ALPHA_CLIP_THRESHOLD = 0.7;
            static const int IRIS_BRIGHTNESS_MODIFIER = 2;
            static const fixed SCLERA_BRIGHTNESS_MODIFIER = 1.2;
            static const fixed LIP_SMOOTHNESS_MULTIPLIER = 0.5;
            static const fixed LIP_SMOOTHNESS_MIN_NDOTL = 0.3;
            static const fixed BROWS_LASHES_DIFFUSEINTENSITY = ONE - 0.25;
            static const int COLOR_MULTIPLIER = 255;
            static const half2 PUPIL_CENTER_UV = half2(0.127, 0.1175);
            static const half DILATION_ENVELOPE = 0.024;
            static const half2 EYE_REGION_UV = PUPIL_CENTER_UV + DILATION_ENVELOPE;

            static const int MASK_SLICE_SIZE = 17;
            static const half MASK_SLICE_THRESHOLD = MASK_SLICE_SIZE * 0.5f;
            static const int MASK_INDEX_IRIS = 255;
            static const int MASK_INDEX_SCLERA = 238;
            static const int MASK_INDEX_LASHES = 221;
            static const int MASK_INDEX_LIPS = 204;
            static const int MASK_INDEX_GUMS = 187;
            static const int MASK_INDEX_TEETH = 170;
            static const int MASK_INDEX_BROWS = 153;

            struct appdata
            {
                float4 vertex: POSITION;
                float3 normal: NORMAL;
                float4 tangent: TANGENT;
                float2 texcoord: TEXCOORD0;
                float4 vertexColor : COLOR0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 uv : TEXCOORD0;
                float4 posWorld: TEXCOORD1;
                float3 normalDir: TEXCOORD2;
                float3 tangentDir: TEXCOORD3;
                float3 bitangentDir: TEXCOORD4;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Calculate tangents for normal mapping
                o.normalDir = normalize(UnityObjectToWorldNormal(v.normal));
                o.tangentDir = normalize(mul(unity_ObjectToWorld, half4(v.tangent.xyz, 0.0)).xyz);
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);

                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv.xy = v.texcoord;
                o.uv.z = v.vertexColor.x * _Slices;
                return o;
            }

            fixed4 frag(v2f i) : COLOR
            {
                // Pupil size offsets uv coords
                if (all(i.uv.xy < EYE_REGION_UV))
                {
                    i.uv.xy -= PUPIL_CENTER_UV;
                    half pupil = saturate(length(i.uv.xy) / DILATION_ENVELOPE);
                    i.uv.xy *= lerp(1.0, pupil, _PupilSize);
                    i.uv.xy += PUPIL_CENTER_UV;
                }

                // Diffuse texture sample
                float4 albedoColor = UNITY_SAMPLE_TEX2DARRAY(_MainTex, i.uv);

                // Process normal map
                float3 transformedNormalUV = i.uv;
                transformedNormalUV.xy = float2(TRANSFORM_TEX(i.uv.xy, _NormalMap));
                float3 normalMap = UNITY_SAMPLE_TEX2DARRAY(_NormalMap, transformedNormalUV) * 2.0 - ONE;
                float3x3 tangentTransform = float3x3(i.tangentDir, i.bitangentDir, i.normalDir);
                float3 normalDirection = normalize(mul(normalMap.rgb, tangentTransform));

                // Roughness contains metallic in r, smoothness in a, mask region in b and mask control in MeshObject
                half4 roughnessTex = UNITY_SAMPLE_TEX2DARRAY(_RoughnessMap, i.uv);

                // Normal/Light/View calculations
                half3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                half VdotN = saturate(dot(viewDirection, normalDirection));
                half NdotL = saturate(dot(normalDirection, normalize(_WorldSpaceLightPos0.xyz)));

                // Sample the default reflection cubemap using the reflection vector
                float3 worldReflection = reflect(-viewDirection, normalDirection);
                half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, worldReflection);
                // Decode cubemap data into actual color
                half3 reflectionColor = DecodeHDR(skyData, unity_SpecCube0_HDR);

                // Get index into texture array
                int componentIndex = floor(i.uv.z + 0.5);

                // Base color from array
                float4 baseColor = _BaseColor[componentIndex];

                // Color space conversions if we are in linear
#ifndef UNITY_COLORSPACE_GAMMA
                _MaskColorIris.rgb = LinearToGammaSpace(_MaskColorIris.rgb);
                _MaskColorLips.rgb = LinearToGammaSpace(_MaskColorLips.rgb);
                _MaskColorBrows.rgb = LinearToGammaSpace(_MaskColorBrows.rgb);
                _MaskColorLashes.rgb = LinearToGammaSpace(_MaskColorLashes.rgb);
                _MaskColorLashesEnd.rgb = LinearToGammaSpace(_MaskColorLashesEnd.rgb);
                _MaskColorSclera.rgb = LinearToGammaSpace(_MaskColorSclera.rgb);
                _MaskColorGums.rgb = LinearToGammaSpace(_MaskColorGums.rgb);
                _MaskColorTeeth.rgb = LinearToGammaSpace(_MaskColorTeeth.rgb);
#endif

                // Calculate color masks
                half irisScalar = abs(roughnessTex.b * COLOR_MULTIPLIER - MASK_INDEX_IRIS) <= MASK_SLICE_THRESHOLD ? roughnessTex.MeshObject : 0.0f;
                half lipsScalar = abs(roughnessTex.b * COLOR_MULTIPLIER - MASK_INDEX_LIPS) <= MASK_SLICE_THRESHOLD ? roughnessTex.MeshObject : 0.0f;
                half browsScalar = abs(roughnessTex.b * COLOR_MULTIPLIER - MASK_INDEX_BROWS) <= MASK_SLICE_THRESHOLD ? roughnessTex.MeshObject : 0.0f;;
                half lashesScalar = abs(roughnessTex.b * COLOR_MULTIPLIER - MASK_INDEX_LASHES) <= MASK_SLICE_THRESHOLD ? roughnessTex.MeshObject : 0.0f;
                half scleraScalar = abs(roughnessTex.b * COLOR_MULTIPLIER - MASK_INDEX_SCLERA) <= MASK_SLICE_THRESHOLD ? roughnessTex.MeshObject : 0.0f;
                half teethScalar = abs(roughnessTex.b * COLOR_MULTIPLIER - MASK_INDEX_TEETH) <= MASK_SLICE_THRESHOLD ? roughnessTex.MeshObject : 0.0f;;
                half gumsScalar = abs(roughnessTex.b * COLOR_MULTIPLIER - MASK_INDEX_GUMS) <= MASK_SLICE_THRESHOLD ? roughnessTex.MeshObject : 0.0f;;

                half3 maskIris = irisScalar * (_MaskColorIris * IRIS_BRIGHTNESS_MODIFIER - baseColor.rgb);
                half3 maskBrows = browsScalar * (_MaskColorBrows - baseColor.rgb);
                half3 maskLashes = lashesScalar * (_MaskColorLashes - baseColor.rgb);
                half3 maskSclera = scleraScalar * (_MaskColorSclera * SCLERA_BRIGHTNESS_MODIFIER - baseColor.rgb);
                half3 maskTeeth = teethScalar * (_MaskColorTeeth - baseColor.rgb);
                half3 maskGums = gumsScalar * (_MaskColorGums - baseColor.rgb);
                // Lip tint excluded from color mask as it lerps with texture color
                half3 colorMask = maskIris + maskBrows + maskLashes + maskSclera + maskTeeth + maskGums;

                // Diffuse intensity from array
                half diffuseIntensity = _DiffuseIntensity[componentIndex];

                // Lerp diffuseIntensity with roughness map
                diffuseIntensity = lerp(diffuseIntensity, ONE, roughnessTex.a);

                // Brows and lashes modify DiffuseIntensity
                diffuseIntensity *= ONE - (saturate(browsScalar + lashesScalar) * BROWS_LASHES_DIFFUSEINTENSITY);
                
                // Add in diffuseIntensity and main lighting to base color
                baseColor.rgb += diffuseIntensity * NdotL * _LightColor0;

                // Add in color mask to base color if this is the head component (index == 0)
                baseColor.rgb += clamp(ONE - componentIndex, 0, ONE) * colorMask;

                // Multiply texture with base color with special case for lips
                albedoColor.rgb = lerp(albedoColor.rgb * baseColor.rgb, _MaskColorLips.rgb, lipsScalar * _MaskColorLips.a);

                // Smoothness multiplier on lip region
                albedoColor.rgb += lipsScalar * reflectionColor * (_LipSmoothness * LIP_SMOOTHNESS_MULTIPLIER) *
                    lerp(LIP_SMOOTHNESS_MIN_NDOTL, ONE, NdotL);

                // Reflection from cubemap
                albedoColor.rgb += reflectionColor * (roughnessTex.a * _ReflectionIntensity[componentIndex]) * NdotL;

                // Rim term
#ifdef SECONDARY_LIGHT_ON
                // Secondary light proxy (direction and color) passed into the rim term
                NdotL = saturate(dot(normalDirection, _SecondaryLightDirection));
                albedoColor.rgb += pow(ONE - VdotN, _RimIntensity[componentIndex]) * NdotL * _SecondaryLightColor;
#else
                albedoColor.rgb += pow(ONE - VdotN, _RimIntensity[componentIndex]) * NdotL;
#endif

                // Global dimmer
                albedoColor.rgb *= _Dimmer;

#if !defined(UNITY_COLORSPACE_GAMMA)
                albedoColor.rgb = GammaToLinearSpace(albedoColor.rgb);
#endif
                albedoColor.rgb = saturate(albedoColor.rgb);

                // Set alpha, with special case for lashes
                albedoColor.a = saturate(albedoColor.a * lerp(ONE, _Alpha, ONE - lashesScalar) * _Alpha);

                // Clip fragments in the lash region for clean lash transparency
                clip(albedoColor.a - lerp(0.0, ALPHA_CLIP_THRESHOLD, lashesScalar));

                // Return clamped final color
                return albedoColor;
            }
            ENDCG
        }
    }
}
