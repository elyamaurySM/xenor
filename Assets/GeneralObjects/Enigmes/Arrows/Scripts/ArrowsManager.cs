using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ArrowsManager : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    public List<GameObject> prefabsL; //prefabs laby
    public List<GameObject> prefabsR; //prefabs room
    public GameObject playerPrefab;
    private const int startX = 0;
    private const int startY = 0;

    private const int x = 12;
    private const int y = 12;

    private int[,] laby = new int[x, y];

    public Transform[] players = new Transform[2]; // Position des 2 joueurs
    public float tileSize = 0.32f; // Taille des tiles du labyrinthe

    int[] player1Tile = new[] { -1, -1 };
    int[] player2Tile = new[] { -1, -1 };

    int[] player1TmpTile = new[] { -1, -1 };
    int[] player2TmpTile = new[] { -1, -1 };

    //coord to spawn
    float cox = startX + 2 * 0.32f;
    float coMx = startX + 6 * 0.32f;
    float coy = startY + 3 * 0.32f;
    void Start()
    {

        GameObject t;
        if (PhotonNetwork.IsMasterClient)
        {
            t = PhotonNetwork.Instantiate(playerPrefab.name, new Vector2(coMx, coy), Quaternion.identity); // Spawn player on network
        }
        else
        {
            t = PhotonNetwork.Instantiate(playerPrefab.name, new Vector2(cox, coy), Quaternion.identity); // Spawn player on network
        }

        GameObject cam = t.transform.GetChild(0).gameObject;
        cam.GetComponent<Camera>().fieldOfView = 120;

        //get player
        players[0] = t.GetComponent<Transform>();

        if (PhotonNetwork.IsMasterClient)
        {

            (int Xt, int Yt) = (0, Random.Range(0, y - 1));
            laby[Xt, Yt] = 2010;//down but not up
            Xt += 1;
            int previous = (int)Direction.down;
            while(Xt < x)
            {
                int r = Random.Range(0, 3);
                if (r == 0) // go down
                {
                    laby[Xt, Yt] = (int)Direction.down+addPrevious(previous);
                    Xt += 1;
                    previous = (int)Direction.down;
                } 
                else if (r == 1 && Yt + 1 < y && previous != (int)Direction.left) //go right
                {
                    laby[Xt, Yt] = (int)Direction.right+addPrevious(previous);
                    Yt += 1;
                    previous = (int) Direction.right;
                } 
                else if (r == 2 && Yt - 1 >= 0 && previous != (int)Direction.right)//go left
                {
                    laby[Xt, Yt] = (int)Direction.left + addPrevious(previous);
                    Yt -= 1;
                    previous = (int) Direction.left;
                }
            }


            createLaby();
        }

    }

    [PunRPC]
    void MoveCoo(float x, float y)
    {
        if (!PhotonNetwork.IsMasterClient)
        {

            players[0].position = new Vector2(x, y);
        }
    }


    public void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (players[1] == null && PhotonNetwork.PlayerList.Length > 1 && GameObject.FindGameObjectsWithTag("Player").Length > 1)
            {
                players[1] = GameObject.FindGameObjectsWithTag("Player")[1].GetComponent<Transform>();
            }


            player1TmpTile[0] = (int)((players[0].position.x - startX) / tileSize);
            player1TmpTile[1] = (int)((players[0].position.y - startY-0.32) / tileSize)*-1-1;

            if(players[1] != null)
            {
                player2TmpTile[0] = (int)((players[1].position.x - startX) / tileSize);
                player2TmpTile[1] = (int)((players[1].position.y - startY - 0.32) / tileSize) * -1-1;
                Debug.Log($"{player2Tile[0]},{player2Tile[1]} to {player2TmpTile[0]},{player2TmpTile[1]}");
            }
            //Debug.Log($"{player1Tile[0]},{player1Tile[1]} to {player1TmpTile[0]},{player1TmpTile[1]}");


            if (player1Tile[0] != player1TmpTile[0] || player1Tile[1] != player1TmpTile[1] || player2Tile[0] != player2TmpTile[0] || player2Tile[1] != player2TmpTile[1])
            {

                if (!CheckPlayerMovement(player1TmpTile, player1Tile) || (players[1] != null && !CheckPlayerMovement(player2TmpTile, player2Tile)))//player break rules, apply consequences
                {
                    Debug.Log("consequences");
                    player1Tile[0] = -1;
                    player1Tile[1] = -1;

                    player2Tile[0] = -1;
                    player2Tile[1] = -1;

                    players[0].position = new Vector2(coMx, coy);

                    if (players[1] != null)
                    {
                        PhotonView photonView = PhotonView.Get(this);
                        photonView.RPC("MoveCoo", RpcTarget.All, cox, coy);
                        //players[1].position = new Vector2(cox, coy);
                    }

                }
                player1Tile[0] = (int)((players[0].position.x - startX) / tileSize); // Calculate position of player in maze
                player1Tile[1] = (int)((players[0].position.y - startY-0.32) / tileSize)*-1-1;

                if(players[1] != null)
                {
                    player2Tile[0] = (int)((players[1].position.x - startX) / tileSize);
                    player2Tile[1] = (int)((players[1].position.y - startY-0.32) / tileSize)*-1-1;
                }

            }

        }
        
    }

    //convert to byteArray on 4 bits 
    private int[] toByteArray(int val)
    {
        if (val > 15) return new int[] { 1, 1, 1, 1 };
        int[] byteArr = new[] { 0, 0, 0, 0 };
        
        for(int i = 0; i < 4; i++)
        {
            byteArr[i] = (val % 2);
            val /= 2;
        }

        //reverse array
        (byteArr[0], byteArr[3]) = (byteArr[3], byteArr[0]);
        (byteArr[1], byteArr[2]) = (byteArr[2], byteArr[1]);

        return byteArr;
    }

    private void printBitArray(int[] bitArr)
    {
        string f = "";
        for (int j = 0; j < bitArr.Length; j++)
        {
            f += bitArr[j].ToString();
        }
        Debug.Log(f);
    }

    //deci to binary but in decimal representation
    private int toBin(int val)
    {
        int[] binArr = toByteArray(val);
        int bin = 0;
        int power = 1;
        for(int i = binArr.Length-1; i >= 0; i--)
        {
            bin += power * binArr[i];
            power *= 10;
        }
        return bin;
    }

 
    //21 => [2,1]
    private int[] DeciToArray(int num, int b = 0)
    {
        List<int> result = new List<int>();
        int len = num.ToString().Length;
        
        while (num != 0)
        {
            result.Insert(0, num % 10);
            num = num / 10;
        }
        for (int i = len; i < b; i++)
        {
            result.Insert(0, 0);
        }

        return result.ToArray();
    }

    //add previous direction to not be opposite
    private int addPrevious(int previous)
    {
        if(previous == -1) return 0;
        if (previous == (int)Direction.right) return 2 * (int)Direction.left;//si avant aller droite mtn peux pas aller gauche
        if (previous == (int)Direction.left) return 2 * (int)Direction.right;
        return 2 * (int)Direction.up; 
    }

    enum Direction
    {
        none = 0000,
        left = 0001,
        down = 0010,
        down_left = 0011,
        right = 0100,
        right_left = 0101,
        right_down = 0110,
        right_down_left = 0111,
        up = 1000,
        up_left = 1001,
        up_down = 1010,
        up_down_left = 1011,
        up_right = 1100,
        up_right_left = 1101,
        up_right_down = 1110,
        up_right_down_left = 1111
    }

    private int prefabRules(int rule)
    {
        List<int> t = new List<int>();
        int tempRule;
        int[] bitArray;
        bool goodNumber;
        if (rule == 0)
        {
            //return new GameObject();
            return Random.Range(1, 15);
        }
        else
        {
            //2010
            for(int i = 1; i < 15; i++)
            {
                tempRule = rule;
                goodNumber = true;
                for (int j =0; j < 4; j++)
                {
                    bitArray = toByteArray(i);
                    if (tempRule%10 == 2 && 1 == bitArray[3-j] )
                    {
                        goodNumber = false;
                    }
                    if(tempRule%10 == 1 && 0 == bitArray[3-j])
                    {
                        goodNumber = false;
                    }
                    tempRule = tempRule / 10;
                }
                if (goodNumber) t.Add(i);
            }


            return t[Random.Range(0, t.Count)];
        }
    }

    private int RandomTopWall() { 
        if(Random.Range(0, 10) < 8)
        {
            return 11;
        }
        else
        {
            return Random.Range(8, 11);
        }
    }
   
    private int RandomRightWall() {
        if (Random.Range(0, 10) < 9)
        {
            return 15;
        }
        return 14;
    }
    
    private int RandomBottomWall()
    {
        if (Random.Range(0, 10) < 8)
        {
            return 7;
        }
        return Random.Range(4, 7);
    }

    private int RandomLeftWall()
    {
        if(Random.Range(0,10) < 9)
        {
            return 13;
        }
        return 12;
    }

    private int RandomFloor()
    {
        if(Random.Range(0, 7) < 6)
        {
            return 16;
        }
        else
        {
            return Random.Range(17, 21);
        }
    }

    private void createLaby()
    {
        var parentObj = new GameObject("ArrowsParent");
        GameObject tmp;
        //floor top and bottom
        for(int j = 0; j < 3; j++)
        {
            for(int i = 0; i < x; i++)
            {
                //Instantiate(prefabsR[RandomFloor()], new Vector3(startX + i * 0.32f + 0.1f, startY + (3-j) * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);//floor top
                //Instantiate(prefabsR[RandomFloor()], new Vector3(startX + i * 0.32f + 0.1f, startY - (2 - j + y) * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);//floor bottom
                PhotonNetwork.Instantiate(prefabsR[RandomFloor()].name, new Vector3(startX + i * 0.32f + 0.1f, startY + (3 - j) * 0.32f - 0.32f), Quaternion.identity);//floor top
                PhotonNetwork.Instantiate(prefabsR[RandomFloor()].name, new Vector3(startX + i * 0.32f + 0.1f, startY - (2 - j + y) * 0.32f - 0.32f), Quaternion.identity);//floor bottom
            }
        }
        //wall left and right
        for(int i = 0; i < x+6; i++)
        {
            //Instantiate(prefabsR[RandomLeftWall()], new Vector3(startX - 1 * 0.32f + 0.1f, startY - (i-3) * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);//left wall
            //Instantiate(prefabsR[RandomRightWall()], new Vector3(startX + x * 0.32f + 0.1f, startY - (i-3) * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);//right wall

            PhotonNetwork.Instantiate(prefabsR[RandomLeftWall()].name, new Vector3(startX - 1 * 0.32f + 0.1f, startY - (i - 3) * 0.32f - 0.32f), Quaternion.identity);//left wall
            PhotonNetwork.Instantiate(prefabsR[RandomRightWall()].name, new Vector3(startX + x * 0.32f + 0.1f, startY - (i - 3) * 0.32f - 0.32f), Quaternion.identity);//right wall
        }

        //angles
        //top left
        //Instantiate(prefabsR[2], new Vector3(startX - 1 * 0.32f + 0.1f, startY + 4 * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);
        PhotonNetwork.Instantiate(prefabsR[2].name, new Vector3(startX - 1 * 0.32f + 0.1f, startY + 4 * 0.32f - 0.32f), Quaternion.identity);

        //top right
        //Instantiate(prefabsR[3], new Vector3(startX + x * 0.32f + 0.1f, startY + 4 * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);
        PhotonNetwork.Instantiate(prefabsR[3].name, new Vector3(startX + x * 0.32f + 0.1f, startY  + 4 * 0.32f - 0.32f), Quaternion.identity);

        //bottom right
        //Instantiate(prefabsR[1], new Vector3(startX + x * 0.32f + 0.1f, startY - ((x + 6) - 3) * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);
        PhotonNetwork.Instantiate(prefabsR[1].name, new Vector3(startX + x * 0.32f + 0.1f, startY - ((x + 6) - 3) * 0.32f - 0.32f), Quaternion.identity);

        //bottom left
        //Instantiate(prefabsR[0], new Vector3(startX - 1 * 0.32f + 0.1f, startY - ((x+6) - 3) * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);
        PhotonNetwork.Instantiate(prefabsR[0].name, new Vector3(startX - 1 * 0.32f + 0.1f, startY - ((x + 6) - 3) * 0.32f - 0.32f), Quaternion.identity);

        //wall top
        for (int i = 0; i < x; i++)
        {
            if(i == 1 || i == 5)
            {
                //tmp = Instantiate(prefabsR[22], new Vector3(startX + i * 0.32f + 0.1f, startY + 4 * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);//door top
                //tmp.GetComponent<SpriteRenderer>().sortingOrder = 1;
                tmp = PhotonNetwork.Instantiate(prefabsR[22].name, new Vector3(startX + i * 0.32f + 0.1f, startY + 4 * 0.32f - 0.32f), Quaternion.identity);//door top
                tmp.GetComponent<SpriteRenderer>().sortingOrder = 1;
            }
            else if(i == x-2 || i == x - 6)
            {
                //tmp = Instantiate(prefabsR[21], new Vector3(startX + i * 0.32f + 0.1f, startY - ((x + 6) - 3) * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);//door bottom
                //tmp.GetComponent<SpriteRenderer>().sortingOrder = 1;
                tmp = PhotonNetwork.Instantiate(prefabsR[21].name, new Vector3(startX + i * 0.32f + 0.1f, startY - ((x + 6) - 3) * 0.32f - 0.32f), Quaternion.identity);//door bottom
                tmp.GetComponent<SpriteRenderer>().sortingOrder = 1;
                //tmp = Instantiate(prefabsR[23], new Vector3(startX + i * 0.32f + 0.1f, startY - ((x + 6) - 4) * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);//tapis bottom
                //tmp.GetComponent<SpriteRenderer>().sortingOrder = 1;
                tmp = PhotonNetwork.Instantiate(prefabsR[23].name, new Vector3(startX + i * 0.32f + 0.1f, startY - ((x + 6) - 4) * 0.32f - 0.32f), Quaternion.identity);//tapis bottom
                tmp.GetComponent<SpriteRenderer>().sortingOrder = 1;
            }
            //Instantiate(prefabsR[RandomTopWall()], new Vector3(startX + i * 0.32f + 0.1f, startY + 4 * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);//wall top
            PhotonNetwork.Instantiate(prefabsR[RandomTopWall()].name, new Vector3(startX + i * 0.32f + 0.1f, startY + 4 * 0.32f - 0.32f), Quaternion.identity);//wall top
            //Instantiate(prefabsR[RandomBottomWall()], new Vector3(startX + i * 0.32f + 0.1f, startY - ((x + 6) - 3) * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);//wall bottom
            PhotonNetwork.Instantiate(prefabsR[RandomBottomWall()].name, new Vector3(startX + i * 0.32f + 0.1f, startY - ((x + 6) - 3) * 0.32f - 0.32f), Quaternion.identity);//wall bottom
        }


        int[,] spawnLaby = new int[x, y];
        int r = Random.Range(0, 3);

        for (int x = 0; x < laby.GetLength(0); x++)
        {
            for (int y = 0; y < laby.GetLength(1); y++)
            {
                r = Random.Range(0, 8);
                if(r == 1 || r == 0) //setup both
                {
                    spawnLaby[x, y] = 2;
                }
                else if(r%2 == 0) //setup local
                {
                    spawnLaby[x, y] = 0;
                }
                else //setup distant
                {
                    spawnLaby[x, y] = 1;
                }
            }
        }


        int prefabNb;
        GameObject t;
        for (int x = 0; x < laby.GetLength(0); x++)
        {
            for (int y = 0; y < laby.GetLength(1); y++)
            {
                prefabNb = prefabRules(laby[x, y]);
                laby[x, y] = toBin(prefabNb);
                if(spawnLaby[x, y] == 0) //setup local
                {
                    Instantiate(prefabsL[prefabNb], new Vector3(startX + y * 0.32f+0.1f, startY - x * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);
                    t = PhotonNetwork.Instantiate(prefabsL[0].name, new Vector3(startX + y * 0.32f + 0.1f, startY - x * 0.32f - 0.32f), Quaternion.identity);
                    t.SetActive(false);
                }
                else if(spawnLaby[x, y] == 1) //setup distant
                {
                    Instantiate(prefabsL[0], new Vector3(startX + y * 0.32f + 0.1f, startY - x * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);
                    t= PhotonNetwork.Instantiate(prefabsL[prefabNb].name, new Vector3(startX + y * 0.32f + 0.1f, startY - x * 0.32f - 0.32f), Quaternion.identity);
                    t.SetActive(false);
                }
                else //both (== 2)
                {
                    //Instantiate(prefabsL[prefabNb], new Vector3(startX + y * 0.32f + 0.1f, startY - x * 0.32f - 0.32f), Quaternion.identity, parentObj.transform);
                    t= PhotonNetwork.Instantiate(prefabsL[prefabNb].name, new Vector3(startX + y * 0.32f + 0.1f, startY - x * 0.32f - 0.32f), Quaternion.identity);
                    //t.SetActive(false);
                }
            }
        }
    }

    private bool RespectRules(int tile, Direction dir)
    {
        int[] tileBitArray = DeciToArray(tile,4);
        int[] dirBitArray = DeciToArray((int) dir, 4);
        for (int i = 0; i < tileBitArray.Length; i++)
        {
            if (tileBitArray[i] < dirBitArray[i]) return false;
        }
        return true;
    }

    private bool CheckPlayerMovement(int[] playerTmpTile, int[] playerTile)
    {
        bool goToOut = false;
        bool fromOut = false;
        //Debug.Log($"From {playerTile[0]},{playerTile[1]} to {playerTmpTile[0]},{playerTmpTile[1]}");

        if (playerTile[1] < 0 || playerTile[0] < 0 || playerTile[1] >= y || playerTile[0] >= x) fromOut = true;
        if (playerTmpTile[1] < 0 || playerTmpTile[0] < 0 || playerTmpTile[1] >= y || playerTmpTile[0] >= x) goToOut = true;

        int tile = 0;
        int newTile = 0;
        if (goToOut && fromOut) return true;
        if (!fromOut)
        {
            tile = laby[playerTile[1], playerTile[0]];
        }
        //else Debug.Log("from out");
        if (!goToOut)
        {
            newTile = laby[playerTmpTile[1], playerTmpTile[0]];
        }
        //else Debug.Log("to out");

        if (playerTile[0] != playerTmpTile[0]) //change x
        {
            if (playerTile[0] > playerTmpTile[0]) //go left
            {
                if (!fromOut && !RespectRules(tile, Direction.left)) return false; //previous tile has not left arrow
                if (!goToOut && RespectRules(newTile, Direction.right)) return false; //current tile has a right arrow
            }
            else //go right
            {
                if (!fromOut && !RespectRules(tile, Direction.right)) return false;
                if (!goToOut && RespectRules(newTile, Direction.left)) return false;
            }
        }
        else if(playerTile[1] != playerTmpTile[1]) //change y
        {
            if (playerTile[1] > playerTmpTile[1]) //go up
            {
                if (!fromOut && !RespectRules(tile, Direction.up)) return false;
                if (!goToOut && RespectRules(newTile, Direction.down)) return false;
            }
            else //go down
            {
                if (!fromOut && !RespectRules(tile, Direction.down)) return false;
                if (!goToOut && RespectRules(newTile, Direction.up)) return false;
            }
        }
        return true;
    }

}
