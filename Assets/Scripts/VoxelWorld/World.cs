using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelWorld
{
    public class World : MonoBehaviour
    {
        public GameObject player;
        public Material textureAtlas;
        public Material fluidTextureAtlas;
        public static ConcurrentDictionary<string, Chunk> chunks;
        public bool initialBuild = true;
        private static readonly List<string> _chunksToRemove = new List<string>();
        public Vector3 lastBuildPosition;
        private static CoroutineQueue _coroutineQueue;

        private static World _instance;

        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            player.SetActive(false);
            Vector3 playerPosition = player.transform.position;
            player.transform.position = new Vector3(playerPosition.x,
                Utils.GenerateHeight(playerPosition.x, playerPosition.z) + 15, playerPosition.z);
            lastBuildPosition = playerPosition;

            initialBuild = true;
            chunks = new ConcurrentDictionary<string, Chunk>();
            Transform localTransform = transform;
            localTransform.position = Vector3.zero;
            localTransform.rotation = Quaternion.identity;


            _coroutineQueue = new CoroutineQueue(Settings.MAX_COROUTINES, StartCoroutine);

            BuildChunkAt((int) (playerPosition.x / Settings.CHUNK_SIZE), (int) (playerPosition.y / Settings.CHUNK_SIZE),
                (int) (playerPosition.z / Settings.CHUNK_SIZE));

            _coroutineQueue.Run(DrawChunks());

            _coroutineQueue.Run(BuildWorldRecursively(
                (int) (playerPosition.x / Settings.CHUNK_SIZE),
                (int) (playerPosition.y / Settings.CHUNK_SIZE),
                (int) (playerPosition.z / Settings.CHUNK_SIZE),
                Settings.RENDER_DISTANCE));
            
            
            InvokeRepeating(nameof(SaveChangedChunks), Settings.AUTOSAVE_DELAY, Settings.AUTOSAVE_DELAY);
        }

        private void Update()
        {
            Vector3 movement = lastBuildPosition - player.transform.position;

            if (movement.magnitude > Settings.CHUNK_SIZE)
            {
                lastBuildPosition = player.transform.position;
                BuildNearPlayer();
            }


            if (!player.activeSelf && initialBuild)
            {
                player.SetActive(true);
                initialBuild = false;
            }

            _coroutineQueue.Run(DrawChunks());
            _coroutineQueue.Run(RemoveOldChunksOutsideRadius());
        }

        public static string BuildChunkName(Vector3 position)
        {
            return (int) position.x + "_" + (int) position.y + "_" + (int) position.z;
        }


        private void BuildChunkAt(int x, int y, int z)
        {
            Vector3 chunkPosition = new Vector3(x * Settings.CHUNK_SIZE, y * Settings.CHUNK_SIZE, z * Settings.CHUNK_SIZE);
            string chunkName = BuildChunkName(chunkPosition);

            if (!chunks.TryGetValue(chunkName, out Chunk chunk))
            {
                chunk = new Chunk(chunkPosition, textureAtlas, fluidTextureAtlas) {world = this};
                Transform localTransform = transform;
                chunk.chunk.transform.parent = localTransform;
                chunk.fluid.transform.parent = localTransform;
                chunks.TryAdd(chunk.chunk.name, chunk);
            }
        }

        private IEnumerator BuildWorldRecursively(int x, int y, int z, int radius)
        {
            radius -= 1;
            if (radius <= 0 || y < 0 || y >= 16) yield break;

            BuildChunkAt(x, y, z - 1);
            _coroutineQueue.Run(BuildWorldRecursively(x, y, z - 1, radius));

            BuildChunkAt(x, y, z + 1);
            _coroutineQueue.Run(BuildWorldRecursively(x, y, z + 1, radius));

            BuildChunkAt(x, y - 1, z);
            _coroutineQueue.Run(BuildWorldRecursively(x, y - 1, z, radius));

            BuildChunkAt(x, y + 1, z);
            _coroutineQueue.Run(BuildWorldRecursively(x, y + 1, z, radius));

            BuildChunkAt(x - 1, y, z);
            _coroutineQueue.Run(BuildWorldRecursively(x - 1, y, z, radius));

            BuildChunkAt(x + 1, y, z);
            _coroutineQueue.Run(BuildWorldRecursively(x + 1, y, z, radius));
            yield return null;
        }

        private IEnumerator DrawChunks()
        {
            foreach (KeyValuePair<string, Chunk> chunk in chunks)
            {
                if (chunk.Value.chunkState == ChunkState.Draw)
                {
                    chunk.Value.DrawChunk();
                }

                if (chunk.Value.chunk &&
                    Vector3.Distance(player.transform.position, chunk.Value.chunk.transform.position) >
                    Settings.RENDER_DISTANCE * Settings.CHUNK_SIZE)
                {
                    _chunksToRemove.Add(chunk.Key);
                }

                yield return null;
            }
        }

        private static IEnumerator RemoveOldChunksOutsideRadius()
        {
            foreach (string chunkName in _chunksToRemove)
            {
                if (chunks.TryGetValue(chunkName, out Chunk chunk))
                {
                    Destroy(chunk.chunk);
                    chunk.Save();
                    chunks.TryRemove(chunkName, out chunk);
                    yield return null;
                }
            }
        }

        private void BuildNearPlayer()
        {
            Vector3 playerPosition = player.transform.position;

            StopCoroutine(nameof(BuildWorldRecursively));
            _coroutineQueue.Run(BuildWorldRecursively(
                (int) (playerPosition.x / Settings.CHUNK_SIZE),
                (int) (playerPosition.y / Settings.CHUNK_SIZE),
                (int) (playerPosition.z / Settings.CHUNK_SIZE),
                Settings.RENDER_DISTANCE));
        }

        public static Block GetWorldBlock(Vector3 position)
        {
            int modX = (int) (position.x % Settings.CHUNK_SIZE);
            int modY = (int) (position.y % Settings.CHUNK_SIZE);
            int modZ = (int) (position.z % Settings.CHUNK_SIZE);

            int blockX = (int) (Mathf.Floor(position.x) % Settings.CHUNK_SIZE) - (position.x < 0 ? 1 : 0);
            int blockY = (int) (Mathf.Floor(position.y) % Settings.CHUNK_SIZE) - (position.y < 0 ? 1 : 0);
            int blockZ = (int) (Mathf.Floor(position.z) % Settings.CHUNK_SIZE) - (position.z < 0 ? 1 : 0);

            int chunkX = (int) Mathf.Floor((int) position.x - modX);
            int chunkY = (int) Mathf.Floor((int) position.y - modY);
            int chunkZ = (int) Mathf.Floor((int) position.z - modZ);

            if (blockX < 0)
            {
                blockX += Settings.CHUNK_SIZE + 1;
                chunkX -= Settings.CHUNK_SIZE;
            }
            
            if (blockY < 0)
            {
                blockY += Settings.CHUNK_SIZE + 1;
                chunkY -= Settings.CHUNK_SIZE;
            }
            
            if (blockZ < 0)
            {
                blockZ += Settings.CHUNK_SIZE + 1;
                chunkZ -= Settings.CHUNK_SIZE;
            }
            
            
            Vector3 chunkPosition = new Vector3(chunkX, chunkY, chunkZ);

            string chunkName = BuildChunkName(chunkPosition);
            return chunks.TryGetValue(chunkName, out Chunk chunk) ? chunk.chunkData[blockX, blockY, blockZ] : null;
        }

        private static IEnumerator SaveChangedChunks()
        {
            Debug.Log("World Saving In Progress...");
            foreach (KeyValuePair<string, Chunk> chunkPair in chunks)
            {
                if (chunkPair.Value.changed) chunkPair.Value.Save();
            }

            Debug.Log("World Saved.");
            yield return null;
        }

        private void OnApplicationQuit()
        {
            StartCoroutine(SaveChangedChunks());
        }

        public static IEnumerator Flow(Block block, BlockType blockType, int strength, int maxSize)
        {
            if (maxSize <= 0) yield break;
            if (block == null) yield break;
            if (strength == 0) yield break;
            if (block.blockSetup.blockType != BlockType.Air) yield break;

            block.SetParent(block.owner.fluid);
            block.SetType(blockType);
            block.blockSetup.health = strength;
            block.owner.Redraw();
            yield return new WaitForSeconds(1);

            int x = (int) block.position.x;
            int y = (int) block.position.y;
            int z = (int) block.position.z;

            Block below = block.GetBlock(x, y - 1, z);
            if (below != null && (below.blockSetup.blockOpacity == BlockOpacity.Transparent || below.blockSetup.blockOpacity == BlockOpacity.Liquid))
            {
                _instance.StartCoroutine(Flow(block.GetBlock(x, y - 1, z), blockType, strength, --maxSize));
            }
            else
            {
                --strength;
                --maxSize;

                _coroutineQueue.Run(Flow(block.GetBlock(x - 1, y, z), blockType ,strength, maxSize));
                yield return new WaitForSeconds(1);
                
                _coroutineQueue.Run(Flow(block.GetBlock(x + 1, y, z), blockType ,strength, maxSize));
                yield return new WaitForSeconds(1);
                
                _coroutineQueue.Run(Flow(block.GetBlock(x, y, z - 1), blockType ,strength, maxSize));
                yield return new WaitForSeconds(1);
                
                _coroutineQueue.Run(Flow(block.GetBlock(x, y, z + 1), blockType ,strength, maxSize));
                yield return new WaitForSeconds(1);
            }
        }

        public static IEnumerator Fall(Block block, BlockType blockType)
        {
            Block thisBlock = block;
            Block previousBlock = null;
            bool isFalling = true;
            bool fall = false;
            
            Block aboveBlock = thisBlock.GetBlock((int) thisBlock.position.x, (int) thisBlock.position.y + 1,
                (int) thisBlock.position.z);

            if (aboveBlock.blockSetup.isFalling)
            {
                Debug.Log("Fall? " + aboveBlock.blockSetup.blockType);
                fall = true;
            }

            while (isFalling)
            {
                
                
                thisBlock.SetParent(thisBlock.owner.chunk);
                BlockType previousType = thisBlock.blockSetup.blockType;
                thisBlock.SetType(blockType);
                if (previousBlock == null) thisBlock.owner.Redraw();

                previousBlock?.SetType(previousType);
                previousBlock?.owner.Redraw();
                
                previousBlock = thisBlock;

                Vector3 position = thisBlock.position;
                thisBlock = thisBlock.GetBlock((int) position.x, (int) position.y - 1, (int) position.z);
                thisBlock.owner.Redraw();
                yield return new WaitForSeconds(.1f);
                                

                if (thisBlock.blockSetup.blockOpacity == BlockOpacity.Solid)
                {
                    thisBlock.owner.Redraw();
                    if (thisBlock.owner != previousBlock.owner) previousBlock.owner.Redraw();
                    isFalling = false;
                }
            }
            
            if (fall) yield return _instance.StartCoroutine(Fall(aboveBlock, aboveBlock.blockSetup.blockType));
        }
    }
}