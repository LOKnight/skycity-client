﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    //棋格的属性
    public float lenth = 1;
    public float size = 1;
    public int chessWidth = 10;
    public int chessHigth = 10;

    public GameObject gridsContent;
    public GameObject unitContent;
    public GameObject grid;
    public GameObject obstacle;  

    public Material gridPoint;
    public Material gridActive;
    public Material gridAttack;
    public Material gridMagic;

    private Queue<UnitAttribute> unitQueue = new Queue<UnitAttribute>();
    private LinkedList<GameObject> unitObject = new LinkedList<GameObject>();
    private Dictionary<Pair,GameObject> grids=new Dictionary<Pair, GameObject>();
    private Stack<Pair> weakGrids = new Stack<Pair>();
    private Queue<Pair> gridQueue = new Queue<Pair>();
    private Stack<Pair> UVStack = new Stack<Pair>();

    private TurnManager turnManager;
    private bool turnFlag = false;
    private bool pathFlag = false;
    private GameObject turnObject;
    private UnitControler turnControler;
    private Pair turnPair;
    private GridPosition mousePosition;
    private bool flag = false;
    private GridPosition.NeighbourGrid neighbourGrid = GridPosition.NeighbourGrid.M;
    private Pair attackUV;
    private bool attackFlag;
    private bool magicFlag=false;
    private bool extinguishFlag = false;

    // Use this for initialization
    void Start () {
        KBEngine.Event.registerOut("SetAllUnit", this, "SetAllUnit");
        KBEngine.Event.registerOut("SetTurnUnit", this, "SetTurnUnit");
        KBEngine.Event.registerOut("UnitListUnderAttack", this, "UnitListUnderAttack");

        turnManager = GameObject.Find("Game Manager").GetComponent<TurnManager>();

        mousePosition = new GridPosition(-1, -1); 
        //生成全部格子
        for (int i=0;i<chessWidth;i++)
            for(int j = 0; j < chessHigth; j++)
            {
                GameObject newGrid = Instantiate(grid, gridsContent.transform);
                newGrid.transform.position = new GridPosition(i, j).GetPosition();
                newGrid.SetActive(false);
                grids.Add(new Pair(i, j), newGrid);
            }
        //生成棋子单位
        //CreatUnit();

        CreatObstacle();

    }
	
	// Update is called once per frame
	void Update () {
        if (turnFlag)
        {
            turnFlag = false;
            turnPair = new Pair(turnControler.GetUV());
            if (attackFlag && turnControler.attribute.attackType != UnitAttribute.AttackType.melee)
                attackFlag = false;
            DrawGrid(turnPair, turnControler.CountLenth);
            pathFlag = true;
        }
        else if(pathFlag)
        {
            MakePath();
        }
        if (flag && turnControler.State == UnitControler.UnitStates.wait)
        {
            if (turnControler.CountLenth != 0)
                turnFlag = true;
            else
            {
                grids[turnControler.GetUV()].GetComponent<GridAttribute>().isUnit = true;
                //turnManager.SetTurnFlag(turnControler.GetUV());
                KBEngine.Event.fireIn("SetTurnFlag", new object[] { turnControler.GetUV().First, turnControler.GetUV().Second });
            }
            flag = false;
        }

    }

    //生成棋子单位
    public void CreatUnit()
    {
        while (unitQueue.Count != 0)
        {
            UnitAttribute attribute = unitQueue.Dequeue();
            GameObject newUnit = (GameObject)Resources.Load("Prefabs/" + attribute.ResourceName);
            Quaternion quaternion = new Quaternion();
            if (attribute.Ascription == UnitAttribute.UnitAscription.blue)
                quaternion = Quaternion.AngleAxis(180f, Vector3.up);
            newUnit = Instantiate(newUnit, new GridPosition(attribute.UV, attribute.Higth).GetPosition(), quaternion, unitContent.transform); //获得创建的object，不然只会修改预设
            newUnit.GetComponent<UnitControler>().attribute = attribute;
            unitObject.AddFirst(newUnit);
            grids[attribute.UV].GetComponent<GridAttribute>().isUnit = true;
        }
    }

    //判断是否在棋盘内
    public bool InChessborad(int u,int v)
    {
        return (u >= 0 && u < chessWidth && v >= 0 && v < chessHigth);
    }
    public bool InChessborad(Pair UV)
    {
        int u = UV.First;
        int v = UV.Second;
        return InChessborad(u, v);
    }

    //创造障碍物
    public void CreatObstacle()
    {
        Pair UV = new Pair(3, 4);
        GameObject obstacleObject = Instantiate(obstacle);
        GridAttribute attribute = grids[UV].GetComponent<GridAttribute>();
        obstacleObject.transform.position = grids[UV].transform.position;
        attribute.isObstacle = true;
        UV = new Pair(4, 5);
        obstacleObject = Instantiate(obstacle);
        attribute = grids[UV].GetComponent<GridAttribute>();
        obstacleObject.transform.position = grids[UV].transform.position;
        attribute.isObstacle = true;
        UV = new Pair(2, 3);
        obstacleObject = Instantiate(obstacle);
        attribute = grids[UV].GetComponent<GridAttribute>();
        obstacleObject.transform.position = grids[UV].transform.position;
        attribute.isObstacle = true;

    }

    //绘制出行动格子  广度优先遍历
    public void DrawGrid(Pair UV,int lenth)
    {
        GameObject gridObject = grids[UV];
        gridObject.SetActive(true);
        gridObject.GetComponent<MeshRenderer>().material = gridPoint;
        gridObject.GetComponent<GridAttribute>().CountLenth = lenth;
        gridObject.GetComponent<GridAttribute>().isActive = true;
        gridQueue.Clear();
        gridQueue.Enqueue(UV);
        while (gridQueue.Count != 0)
        {
            FindRoad(gridQueue.Dequeue()); 
        }

        if(!attackFlag)   //绘制远程可攻击的格子
        {
            LinkedListNode<GameObject> node = unitObject.First;
            Pair uv;
            while (node != null)
            {
                if (node.Value.GetComponent<UnitControler>().Ascription!=turnControler.Ascription)
                {
                    uv = node.Value.GetComponent<UnitControler>().GetUV();
                    gridObject = grids[uv];
                    gridObject.SetActive(true);
                    gridObject.GetComponent<GridAttribute>().isActive = true;
                    gridObject.GetComponent<MeshRenderer>().material = gridAttack;
                    weakGrids.Push(uv);
                }
                node = node.Next;
            }
        }
    }

    private void FindRoad(Pair UV)
    {
        GridPosition gridPosition = new GridPosition(UV);
        int lenth = grids[UV].GetComponent<GridAttribute>().CountLenth - 1;
        //顺时针
        WeakGrid(gridPosition.N, lenth, UV);
        WeakGrid(gridPosition.NE, lenth, UV);
        WeakGrid(gridPosition.E, lenth, UV);
        WeakGrid(gridPosition.SE, lenth, UV);
        WeakGrid(gridPosition.S, lenth, UV);
        WeakGrid(gridPosition.SW, lenth, UV);
        WeakGrid(gridPosition.W, lenth, UV);
        WeakGrid(gridPosition.NW, lenth, UV);
    }

    private bool WeakGrid(Pair UV, int lenth,Pair p)   //激活格子同时预设路径
    {
        if (!InChessborad(UV))
            return false;
        GameObject gridObject = grids[UV]; 
        GridAttribute attribute = gridObject.GetComponent<GridAttribute>();
        if (attribute.isObstacle)
            return false;
        if (attribute.isUnit)
        {
            if (!attackFlag && lenth == turnControler.CountLenth - 1&& FindUnitByUV(UV).GetComponent<UnitControler>().Ascription != turnControler.Ascription)
                attackFlag = true;
            if(attackFlag&&FindUnitByUV(UV).GetComponent<UnitControler>().Ascription!=turnControler.Ascription)
            {
                gridObject.SetActive(true);
                gridObject.GetComponent<GridAttribute>().isActive = true;
                gridObject.GetComponent<MeshRenderer>().material = gridAttack;
                weakGrids.Push(UV);
                return true;
            }
            return false;
        }
        if(attribute.CountLenth >= 0)
        {
            if (lenth >= attribute.CountLenth&&(p.First==UV.First||p.Second==UV.Second))   //期望走直线
                attribute.ParentUV = p;
            return false;
        }
        gridObject.SetActive(true);
        gridObject.GetComponent<GridAttribute>().isActive = true;
        attribute.CountLenth = lenth; 
        attribute.ParentUV = p;  //存储父节点
        weakGrids.Push(UV);
        if(lenth>0)
            gridQueue.Enqueue(UV);
        return true;
    }

    public void MakePath()
    {
        if (Input.mousePresent)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 300f))  //Physics.Raycast(ray, out hit, 300f, LayerMask.NameToLayer("Chess Grid")) 按理说可以过滤其他层，可是这时只有离摄像头近的有效，很奇怪
            {
                Debug.DrawLine(ray.origin, hit.point);
                GridPosition newPosition = new GridPosition(hit.point);
                Pair UV = new Pair(newPosition);
                if (!UV.Equals(turnPair) && grids[UV].activeSelf && grids[UV].GetComponent<GridAttribute>().isActive/*hit.collider.CompareTag("Grid") 效果不好*/)  //Pair的其他值可能不一样要用自定义的Equals
                {
                    if (!mousePosition.Equals(newPosition))
                    {
                        if (magicFlag)
                        {
                            magicFlag = false;
                            extinguishFlag = true;
                            turnControler.exceptMelee();
                        }
                        mousePosition = newPosition;
                        ExtinguishPath();
                        if (!grids[UV].GetComponent<GridAttribute>().isUnit)
                            LightPath(new Pair(mousePosition));
                        if (neighbourGrid != GridPosition.NeighbourGrid.M)
                            neighbourGrid = GridPosition.NeighbourGrid.M;
                    }
                    else if (grids[UV].GetComponent<GridAttribute>().isUnit)      //处理敌方单位
                    {
                        if (attackFlag)  //近战攻击
                        {
                            if (Input.GetMouseButton(0))    //进行攻击
                            {
                                turnControler.MoveUnit(gridQueue.ToArray());
                                attackUV = new Pair(mousePosition);
                                turnControler.AttackUnit(attackUV);
                                ExtinguishPath();
                                SleepGrid();
                                flag = true;
                            }
                            else                            //攻击前选择方向
                            {
                                Vector3 gridVector = mousePosition.GetPosition();
                                if (hit.point.x < gridVector.x - 0.16f && hit.point.z < gridVector.z - 0.16f)
                                    LightPathByNeighbour(GridPosition.NeighbourGrid.SW);
                                else if (hit.point.x + hit.point.z <= 1f && hit.point.x <= gridVector.x + 0.16f && hit.point.z <= gridVector.z + 0.16f)
                                {
                                    if (hit.point.x >= hit.point.z)
                                        LightPathByNeighbour(GridPosition.NeighbourGrid.S);
                                    else
                                        LightPathByNeighbour(GridPosition.NeighbourGrid.W);
                                }
                                else if (hit.point.x < gridVector.x - 0.16f && hit.point.z > gridVector.z + 0.16f)
                                    LightPathByNeighbour(GridPosition.NeighbourGrid.NW);
                                else if (hit.point.x > gridVector.x + 0.16f && hit.point.z < gridVector.z - 0.16f)
                                    LightPathByNeighbour(GridPosition.NeighbourGrid.SE);
                                else if (hit.point.x > gridVector.x + 0.16f && hit.point.z > gridVector.z + 0.16f)
                                    LightPathByNeighbour(GridPosition.NeighbourGrid.NE);
                                else
                                {
                                    if (hit.point.x >= hit.point.z)
                                        LightPathByNeighbour(GridPosition.NeighbourGrid.E);
                                    else
                                        LightPathByNeighbour(GridPosition.NeighbourGrid.N);
                                }
                            }
                        }
                        else                                              //远程攻击
                            turnControler.exceptMelee();
                    }
                    else if (Input.GetMouseButton(0))
                    {
                        turnControler.MoveUnit(gridQueue.ToArray());
                        ExtinguishPath();
                        SleepGrid();
                        flag = true;
                    }
                }
                else
                {
                    ExtinguishPath();
                    if (magicFlag)
                    {
                        magicFlag = false;
                        extinguishFlag = true;
                        turnControler.exceptMelee();
                    }
                }
            }
        }
    }

    private void LightPath(Pair UV)
    {
        if (UV != turnPair)    //由于两个点完全一样等于号成立
        {
            GameObject gridObject = grids[UV];
            gridObject.GetComponent<MeshRenderer>().material = gridPoint;
            gridQueue.Enqueue(UV);
            LightPath(gridObject.GetComponent<GridAttribute>().ParentUV);
        }
    }

    private void LightPathByNeighbour(GridPosition.NeighbourGrid neighbourGrid)
    {
        if (this.neighbourGrid != neighbourGrid)
        {
            this.neighbourGrid = neighbourGrid;
            ExtinguishPath();
            if (weakGrids.Contains(mousePosition.GetNeighbour(neighbourGrid)))
                LightPath(mousePosition.GetNeighbour(neighbourGrid));
        }
    }

    private void ExtinguishPath()
    {
        while (gridQueue.Count != 0)
        {
            grids[gridQueue.Dequeue()].GetComponent<MeshRenderer>().material = gridActive;
        }
    }

    private void SleepGrid()
    {
        Pair p= turnControler.GetUV();
        weakGrids.Push(p);
        while (weakGrids.Count != 0)
        {
            p = weakGrids.Pop();
            grids[p].GetComponent<MeshRenderer>().material = gridActive;
            grids[p].SetActive(false);
            grids[p].GetComponent<GridAttribute>().isActive = false;
            grids[p].GetComponent<GridAttribute>().CountLenth = -1;
        }
        pathFlag = false;
    }

    //各种其他攻击方法
    public void LongRangeAttack()     //普通远程攻击
    {
        if (Input.GetMouseButton(0))    //进行攻击
        {
            attackUV = new Pair(mousePosition);
            turnControler.AttackUnit(attackUV);
            ExtinguishPath();
            SleepGrid();
            flag = true;
        }
    }

    public void MagicAttack()
    {
        if (extinguishFlag)
        {
            ToExtinguishMagicGrid();
            UVStack.Clear();
        }
        else
        {
            if (Input.GetMouseButton(0))    //进行攻击
            {
                attackUV = new Pair(mousePosition);
                turnControler.AttackUnit(attackUV);
                magicFlag = false;
                ToExtinguishMagicGrid();
                ExtinguishPath();
                SleepGrid();
                flag = true;
            }
            else if(!magicFlag)             //显示攻击造成伤害范围
            {
                LightMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.N));
                LightMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.NE));
                LightMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.E));
                LightMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.SE));
                LightMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.S));
                LightMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.SW));
                LightMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.W));
                LightMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.NW));
                magicFlag = true;
            }
        }
    }

    //各种其他攻击方法

    private void LightMagicGrid(Pair UV)
    {
        if (!InChessborad(UV))
            return;
        GameObject gridObject = grids[UV];
        if (gridObject.GetComponent<GridAttribute>().isObstacle)
            return;
        GridAttribute attribute = gridObject.GetComponent<GridAttribute>();
        attribute.PreGridMaterial = gridObject.GetComponent<MeshRenderer>().material;
        attribute.PreActive = gridObject.activeSelf;
        if (attribute.isUnit)
            UVStack.Push(UV);
        if (!attribute.isUnit||FindUnitByUV(UV).GetComponent<UnitControler>().Ascription==turnControler.Ascription)
            gridObject.GetComponent<MeshRenderer>().material = gridMagic;
        if (!gridObject.activeSelf)
            gridObject.SetActive(true);
    }

    private void ToExtinguishMagicGrid()
    {
        ExtinguishMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.N));
        ExtinguishMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.NE));
        ExtinguishMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.E));
        ExtinguishMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.SE));
        ExtinguishMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.S));
        ExtinguishMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.SW));
        ExtinguishMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.W));
        ExtinguishMagicGrid(mousePosition.GetNeighbour(GridPosition.NeighbourGrid.NW));
        extinguishFlag = false;
    }

    private void ExtinguishMagicGrid(Pair UV)
    {
        if (!InChessborad(UV))
            return;
        GameObject gridObject = grids[UV];
        if (gridObject.GetComponent<GridAttribute>().isObstacle)
            return;
        gridObject.GetComponent<MeshRenderer>().material = gridObject.GetComponent<GridAttribute>().PreGridMaterial;
        gridObject.SetActive(gridObject.GetComponent<GridAttribute>().PreActive);
    }

    //向服务器传送那个物体被攻击了，有服务器处理
    public void ToAttackUnit()
    {
        UVStack.Push(attackUV);
        //turnManager.UnitAttack(UVStack.ToArray(),attackFlag);
        KBEngine.Event.fireIn("UnitAttack", new object[] { UVStack.ToArray(), attackFlag });
    }

    /// <summary>
    /// 获得服务器处理结果
    /// </summary>
    /// <param name="damage">收到的伤害值</param>
    /// <param name="deadFlag">死亡标记</param>
    public void UnitUnderAttack(int damage,bool deadFlag)
    {
        Pair UV = UVStack.Pop();
        GameObject underAttackUnit = FindUnitByUV(UV);
        underAttackUnit.GetComponent<UnitControler>().UnderAttack(damage, deadFlag);
        if (deadFlag)
        {
            grids[UV].GetComponent<GridAttribute>().isUnit = false;
            unitObject.Remove(underAttackUnit);
        }
    }
    public void UnitListUnderAttack(Dictionary<string, object> infos)
    {
        List<object> listinfos = (List<object>)infos["values"];
        foreach (Dictionary<string, object> info in listinfos)
             UnitUnderAttack(System.Convert.ToInt32(info["damage"]), System.Convert.ToBoolean(info["deadFlag"]));
        UVStack.Clear();
    }

    //由服务器（现在时模拟服务器的脚本TurnManager）传输数据初始化要显示的棋子的队列
    public void SetUnit(UnitAttribute unitAttribute)
    {
        unitQueue.Enqueue(unitAttribute);
    }

    public void SetAllUnit(Dictionary<string, object> infos)
    {
        List<object> listinfos = (List<object>)infos["values"];
        UnitAttribute unitAttribute;
        for (int i = 0; i < listinfos.Count; i++)
        {
            unitAttribute = new UnitAttribute((Dictionary<string, object>)listinfos[i]);
            unitQueue.Enqueue(unitAttribute);
        }

        CreatUnit();
    }

    //通过UV找到相应的棋子
    private GameObject FindUnitByUV(Pair UV)
    {
        GameObject foundUnit = null;
        LinkedListNode<GameObject> node = unitObject.First;
        while(node!=null)
        {
            if (node.Value.GetComponent<UnitControler>().GetUV().Equals(UV))
            {
                foundUnit = node.Value;
                break;
            }
            node = node.Next;
        }
        return foundUnit;
    }

    //由服务器（现在时模拟服务器的脚本TurnManager）传输数据找到本回合要移动的棋子
    public void SetTurnUnit(UnitAttribute attribute)
    {
        turnObject = FindUnitByUV(attribute.UV);
        if (turnObject == null)
            Debug.Log("unit can not find!");
        else
        {
            turnControler = turnObject.GetComponent<UnitControler>();
            turnControler.SetCountLenth();
            if (turnControler.attribute.attackType == UnitAttribute.AttackType.melee)
                attackFlag = true;
            else
                attackFlag = false;
            grids[attribute.UV].GetComponent<GridAttribute>().isUnit = false;
            turnFlag = true;
        }
    }
}
