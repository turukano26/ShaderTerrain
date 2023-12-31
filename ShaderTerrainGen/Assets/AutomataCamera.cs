using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AutomataCamera : MonoBehaviour
{
    public int buffsize;
    public ComputeShader compute;
    RenderTexture[] textures;
    ComputeBuffer buffer;

    public int i;
    public int prev;
    public int index;

    RenderTexture[] finishedTextures;
    public int curRenderedTexture;

    public ComputeShader perlinShader;

    public int mapSize;

    public int numOfPlates;


    // Start is called before the first frame update
    void Start()
    {
        int i = 0;

        buffer = new ComputeBuffer(1, 4);

        textures = new RenderTexture[buffsize];
        for (int j = 0; j < buffsize; j++)
        {
            textures[i] = new RenderTexture(mapSize, mapSize, 24);
            textures[i].enableRandomWrite = true;
            textures[i].Create();
            i++;
        }
        int kernel = compute.FindKernel("CSMain");
        compute.SetTexture(kernel, "Result", textures[0]);
        compute.SetVector("Result_TexelSize", new Vector4(1.0f / mapSize, 1.0f / mapSize, mapSize, mapSize));
        compute.Dispatch(kernel, mapSize / 8, mapSize / 8, 1);

        


        finishedTextures = new RenderTexture[6];
        for (int j = 0; j < 6; j++)
        {
            finishedTextures[j] = new RenderTexture(mapSize, mapSize, 24);
            finishedTextures[j].enableRandomWrite = true;
            finishedTextures[j].Create();
        }


        Texture2D seedRendTex = new Texture2D(mapSize, mapSize);
        Color[] BlackPixels = Enumerable.Repeat(Color.clear, mapSize * mapSize).ToArray();
        seedRendTex.SetPixels(BlackPixels);

        for (int j = 0; j < numOfPlates; j++)
        {
            int r1 = (int) (Random.Range(0f, 1f) * mapSize);
            int r2 = (int) (Random.Range(0f, 1f) * mapSize);
            print(r1);
            seedRendTex.SetPixel(r1, r2, new Color((j + 1) / (numOfPlates + 1), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1f));
        }

        seedRendTex.Apply();

        Graphics.Blit(seedRendTex, textures[0]); 



        GeneratePerlin();

        Graphics.CopyTexture(textures[0], finishedTextures[0]);

        for (int j = 0; j < 500; j++)
        {
            //RunContinentGen();
        }

        finishedTextures[1] = textures[index];
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            RunContinentGen();
            finishedTextures[1] = textures[index];
        }

        if (Input.GetKeyDown("right"))
        {
            curRenderedTexture++;
        }

        if (Input.GetKeyDown("left"))
        {
            curRenderedTexture--;
        }
        if (Input.GetKeyDown("space"))
        {
            RunOceans();
            RunVolcanism();
            AddTextures(finishedTextures[3], finishedTextures[4], finishedTextures[5]);
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {        
        Graphics.Blit(finishedTextures[curRenderedTexture % finishedTextures.Length], dest);
    }

    private static Vector2 GetRandomDirection()
    {
        return new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized;
    }

    void GeneratePerlin()
    {
        int perlinKernel = perlinShader.FindKernel("CSMain");
        perlinShader.SetTexture(perlinKernel, "Result", finishedTextures[2]);

        ComputeBuffer gradients = new ComputeBuffer(mapSize, sizeof(float) * 2);
        gradients.SetData(Enumerable.Range(0, mapSize).Select((i) => GetRandomDirection()).ToArray());


        perlinShader.SetFloat(Shader.PropertyToID("res"), 64.0f);
        perlinShader.SetFloat(Shader.PropertyToID("t"), 0.8f);
        perlinShader.SetBuffer(perlinKernel, Shader.PropertyToID("gradients"), gradients);

        perlinShader.Dispatch(perlinKernel, mapSize / 8, mapSize / 8, 1);
    }

    void RunContinentGen()
    {
        prev = index;
        i++;
        index = i % buffsize;

        int klife = compute.FindKernel("GameOfLife");

        compute.SetBuffer(klife, Shader.PropertyToID("done"), buffer);
        compute.SetTexture(klife, "Prev", textures[prev]);
        compute.SetTexture(klife, "Result", textures[index]);
        compute.SetTexture(klife, "Perlin", finishedTextures[2]);
        compute.Dispatch(klife, textures[0].width / 8, textures[0].height / 8, 1);

        int[] bufferArray = { 0 };

        buffer.GetData(bufferArray);

        print(bufferArray[0]);
    }

    void RunOceans()
    {
        int oceanKernel = compute.FindKernel("Oceans");

        compute.SetTexture(oceanKernel, "Continents", textures[index]);
        compute.SetTexture(oceanKernel, "Result", finishedTextures[3]);
        compute.Dispatch(oceanKernel, textures[0].width / 8, textures[0].height / 8, 1);
    }

    void RunVolcanism()
    {
        int volcanismKernel = compute.FindKernel("Volcanism");

        compute.SetTexture(volcanismKernel, "Plates", textures[index]);
        compute.SetTexture(volcanismKernel, "VolcanismResult", finishedTextures[4]);
        compute.Dispatch(volcanismKernel, textures[0].width / 8, textures[0].height / 8, 1);
    }

    void AddTextures(RenderTexture input1, RenderTexture input2, RenderTexture output)
    {
        int addKernel = compute.FindKernel("AddTextures");

        compute.SetTexture(addKernel, "Input1", input1);
        compute.SetTexture(addKernel, "Input2", input2);
        compute.SetTexture(addKernel, "Output", output);
        compute.Dispatch(addKernel, textures[0].width / 8, textures[0].height / 8, 1);
    }
}
