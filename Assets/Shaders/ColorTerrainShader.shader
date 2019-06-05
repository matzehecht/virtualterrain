Shader "Unlit/ColorTerrainShader"
{
    Properties
    {
        // Texture for the height colormap
        _ColorTex ("Color Texture", 2D) = "white" {}
        // the maximum height of the terrain
        _maxTerrainHeight("Max Terrain Height", float) = 5

        // binary texture for water identification
        _WaterTex ("Water Texture", 2D) = "white" {}
        // color of the water
        _WaterColor ("Water Color", Color) = (0, 0, 1, 1)

        // Reflectance of the ambient light
		_Ka("Ambient Reflectance", Range(0, 1)) = 0.5
		// Reflectance of the diffuse light
		_Kd("Diffuse Reflectance", Range(0, 1)) = 0.5

        // Specular reflectance for water surface
		_Ks("Specular Reflectance", Range(0, 1)) = 0.8
		// Shininess for specular water surface
		_Shininess("Shininess", Range(1, 50)) = 8

        _NormalMap1("Normal Map 1", 2D) = "bump" {}
        _NormalMap2("Normal Map 2", 2D) = "bump" {}
        _ScrollSpeedX("Scroll Speed X", Range(0,2)) = 0.2
        _ScrollSpeedY("Scroll Speed Y", Range(0,2)) = 0.2
    }
    SubShader
    {
        Pass
        {
            // indicate that our pass is the "base" pass in forward
            // rendering pipeline. It gets ambient and main directional
            // light data set up; light direction in _WorldSpaceLightPos0
            // and color in _LightColor0
            Tags {"LightMode"="ForwardBase"}

            CGPROGRAM
            // definition of used shaders and their names
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc" // for lightning

            // input struct of vertex shader
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
                float4 tangent : TANGENT;
            };

            // struct for data from vertex to fragment shader
            struct v2f
            {
                // vertex positions in homogeneous coordinates
                float4 pos : SV_POSITION;
                // base vertice color
                float4 color : COLOR0;
                // water texture (binary)
                float4 watTexVal : TEXCOORD0;
                // surface normals
				half3 worldViewDir : TEXCOORD1;
                half3 worldNormal : TEXCOORD2;

                half3 tspace0 : TEXCOORD3;
                half3 tspace1 : TEXCOORD4;
                half3 tspace2 : TEXCOORD5;
                
                float2 uv : TEXCOORD6;
                float3 worldPos : TEXCOORD7;
            };

            // variable definitions
            float _Ka, _Kd, _Ks, _maxTerrainHeight, _Shininess;
            float _ScrollSpeedX, _ScrollSpeedY;
            sampler2D _ColorTex, _WaterTex, _NormalMap1, _NormalMap2;
            float4 _WaterColor;

            // VERTEX SHADER
            v2f vert (appdata vertexIn)
            {
                v2f vertexOut;
                // transform vertices from object coordinates to clip coordinates
                vertexOut.pos = UnityObjectToClipPos(vertexIn.vertex);
                // get normalized world space direction from given object space vertex position towards the camera
                vertexOut.worldViewDir = normalize(WorldSpaceViewDir(vertexIn.vertex));
                vertexOut.worldPos = mul(unity_ObjectToWorld, vertexIn.vertex);

                // transform normal vectors to world coordinates
                half3 worldNormal = UnityObjectToWorldNormal(vertexIn.normal);
                vertexOut.worldNormal = worldNormal;

                half3 worldTangent = UnityObjectToWorldDir(vertexIn.tangent);
                
                // compute bitangent from cross product of normal and tangent
				// bitanget vector is needed to convert the normal from the normal map into world space
                half tangentSign = vertexIn.tangent.w * unity_WorldTransformParams.w;
                half3 worldBitangent = cross(worldNormal, worldTangent) * tangentSign;

  	            // output the tangent space matrix
				vertexOut.tspace0 = half3(worldTangent.x, worldBitangent.x, worldNormal.x);
				vertexOut.tspace1 = half3(worldTangent.y, worldBitangent.y, worldNormal.y);
				vertexOut.tspace2 = half3(worldTangent.z, worldBitangent.z, worldNormal.z);
                
                // access water surface texture and extract color value (binary)
                float4 watTexVal = tex2Dlod(_WaterTex, float4(vertexIn.texcoord.xy, 0, 0));
                vertexOut.watTexVal = watTexVal;

                // access colormap texture and extract color values (based on height of the vertex divided by the maximum height of the terrain)
                float4 colTexVal = tex2Dlod(_ColorTex, float4(vertexIn.vertex.y / _maxTerrainHeight, 0, 0, 0));
                // set color based on the water texture (if 0, then take height color, otherwise the water color)
                // enough to check one color value (red) of the texture, because it's binary and eithor black or white
                if(vertexOut.worldPos.y <= 0) {
                    vertexOut.color = _WaterColor;
                }
                else {
                    vertexOut.color = colTexVal;
                }

                vertexOut.uv = vertexIn.texcoord;

                return vertexOut;
            }
            
            // FRAGMENT SHADER
            float4 frag (v2f fragIn) : SV_Target {
                // set base colors
                float4 color = fragIn.color;

                // calculate offset based on elapsed time for water simulation5
                float offsetX = _ScrollSpeedX * _Time;
                float offsetY = _ScrollSpeedY * _Time;

                // sample the first and second normal map, and decode from the Unity encoding
                // add the calculated time offset to the normal maps (move the first normal map on x axis; second on y axis)
				half3 tnormal1 = UnpackNormal(tex2D(_NormalMap1, fragIn.uv + float2(offsetX, 0))); 
				half3 tnormal2 = UnpackNormal(tex2D(_NormalMap2, fragIn.uv + float2(0, offsetY)));

                // transform normal from tangent to world space (take both normal maps into account)
                half3 worldNormal;
                if(fragIn.worldPos.y <= 0) {
                    worldNormal.x = dot(fragIn.tspace0, (tnormal1 + tnormal2)/2);
                    worldNormal.y = dot(fragIn.tspace1, (tnormal1 + tnormal2)/2);
                    worldNormal.z = dot(fragIn.tspace2, (tnormal1 + tnormal2)/2);
                }
                else {
                    worldNormal = fragIn.worldNormal;
                }

                // calculate ambient light color
                float4 amb = float4(ShadeSH9(half4(worldNormal,1)),1);
                // calculate diffuse light color
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                float4 diff = nl * _LightColor0;

                // multiply base color with ambient and diffuse light
                color *= (_Ka * amb + _Kd * diff);
                
                // only add specular phong shading (specular surface), if it's a water surface
                if(fragIn.worldPos.y <= 0) {   
                    float3 worldSpaceReflection = reflect(normalize(-_WorldSpaceLightPos0.xyz), worldNormal);
                    half re = pow(max(dot(worldSpaceReflection, fragIn.worldViewDir), 0), _Shininess);
                    float4 spec = re * _LightColor0;
                    color += _Ks * spec;
                }

                // calculate and paint height lines to the terrain
                float heightLinePnt = 0.02;

                if(fragIn.worldPos.y % 0.5 < heightLinePnt && fragIn.worldPos.y > heightLinePnt) {
                    color.rgb = float3(0, 0, 0);
                }

                return saturate(color);
            }

            ENDCG
        }
    }
}
