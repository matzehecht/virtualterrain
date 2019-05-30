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
            };

            // struct for data from vertex to fragment shader
            struct v2f
            {
                // vertex positions in homogeneous coordinates
                float4 pos : SV_POSITION;
                // base vertice color
                float4 color : COLOR0;
                // ambient light color
                float4 amb : COLOR1;
                // diffuse light color
                float4 diff : COLOR2;
                // water texture (binary)
                float4 watTexVal : TEXCOORD0;
                // surface normals
                half3 worldNormal : TEXCOORD1;
				half3 worldViewDir : TEXCOORD2;
            };

            // variable definitions
            float _Ka, _Kd, _Ks, _maxTerrainHeight, _Shininess;
            sampler2D _ColorTex, _WaterTex;
            float4 _WaterColor;

            // VERTEX SHADER
            v2f vert (appdata vertexIn)
            {
                v2f vertexOut;
                // transform vertices from object coordinates to clip coordinates
                vertexOut.pos = UnityObjectToClipPos(vertexIn.vertex);

                // transform normal vectors to world coordinates
                half3 worldNormal = UnityObjectToWorldNormal(vertexIn.normal);
                vertexOut.worldNormal = worldNormal;
                // get normalized world space direction from given object space vertex position towards the camera
                vertexOut.worldViewDir = normalize(WorldSpaceViewDir(vertexIn.vertex));

                // calculate ambient light color
                vertexOut.amb = float4(ShadeSH9(half4(worldNormal,1)),1);

                // calculate diffuse light color
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                vertexOut.diff = nl * _LightColor0;
                
                // access water surface texture and extract color value (binary)
                float4 watTexVal = tex2Dlod(_WaterTex, float4(vertexIn.texcoord.xy, 0, 0));
                vertexOut.watTexVal = watTexVal;

                // access colormap texture and extract color values (based on height of the vertex divided by the maximum height of the terrain)
                float4 colTexVal = tex2Dlod(_ColorTex, float4(vertexIn.vertex.y / _maxTerrainHeight, 0, 0, 0));
                // set color based on the water texture (if 0, then take height color, otherwise the water color)
                // enough to check one color value (red) of the texture, because it's binary and eithor black or white
                if(watTexVal.r == 0) {
                    vertexOut.color = colTexVal;
                }
                else {
                    vertexOut.color = _WaterColor;
                }

                return vertexOut;
            }
            
            // FRAGMENT SHADER
            float4 frag (v2f fragIn) : SV_Target {
                // set base colors
                float4 color = fragIn.color;
                // multiply base color with ambient and diffuse light
                color *= (_Ka * fragIn.amb + _Kd * fragIn.diff);
                
                // only add specular phong shading (specular surface), if it's a water surface
                if(fragIn.watTexVal.r == 1) {
                    
                    float3 worldSpaceReflection = reflect(normalize(-_WorldSpaceLightPos0.xyz), fragIn.worldNormal);
                    half re = pow(max(dot(worldSpaceReflection, fragIn.worldViewDir), 0), _Shininess);
                    float4 spec = re * _LightColor0;
                    color += _Ks * spec;
                }

                return saturate(color);
            }

            ENDCG
        }
    }
}
