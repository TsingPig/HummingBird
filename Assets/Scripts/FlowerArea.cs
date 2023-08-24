using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������(FlowerPlant)���б���ӻ�
/// </summary>
public class FlowerArea : MonoBehaviour
{
    //Agent�������壩�ͻ�����ʹ�õ������ֱ��
    //���ڹ淶 Agent�뻨����Ծ���
    public const float areaDiameter = 20f;

    public Vector3 FlowerAreaCenter
    {
        get { return transform.position; }
    }
    //���ص��б�
    private List<GameObject> flowerPlants;

    /// <summary>
    /// <see cref="Flower"/>�б��ⲿ��ȡ���ڲ��޸ĵ�����������С���ܹ��޸�
    /// </summary>
    public List<Flower> Flowers
    {
        get;
        private set;
    }


    //���۴����� -> ����Flower��������ֵ�
    //������С���ڴ�����ĳһ�����۵�ʱ���ҵ���Ӧ�Ļ�
    //������Nectar��ǩ��tag������С������һ�������¼���ʱ��
    //���ǻ������ж��Ƿ��Ѿ�ײ����һ������(NectarTagCollider)
    private Dictionary<Collider, Flower> nectarFlowerDictionary;



    /// <summary>
    /// ���û��ͻ���
    /// </summary>
    public void ResetFlowerPlants()
    {
        //����Y����תÿһ�ػ��أ�������X��Z��΢��ת
        foreach (var flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5f);
            float yRotation = UnityEngine.Random.Range(-180f, 180f);
            float zRotation = UnityEngine.Random.Range(-5f, 5f);

            /*
            ������Ԫ��ѡ��

            (1)ŷ���ǵ���Ԫ����ת��
            Quaternion quaternion = Quaternion.Euler(xRotation, yRotation, zRotation);
            

            (2)��ת������Ԫ����forward�������ǰ��ʸ����upwards���Ϸ���ʸ������
            Quaternion quaternion = Quaternion.LookRotation(forward, upwards);
            ʹ��������ת������forward��ͷ������upwards
            
            (3)��Ƕ�ѡ��angle�ǽǶȣ�axis��Ҫ��ת���ᣩ
            Quaternion quaternion = Quaternion.AngleAxis(angle, axis);
            
            (4)��ֵ��ת��������ʼ��Ԫ����Ŀ����Ԫ����t����0~1֮��)
            Quaternion result = Quaternion.Lerp(startQuaternion, targetQuaternion, t);

             */
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation,
                yRotation, zRotation);

        }

        //�ָ�ÿһ�仨����ײ��
        foreach (var flower in Flowers)
        {
            flower.ResetFlower();
        }
    }


    /// <summary>
    /// ��û��۶�Ӧ��<see cref="Flower"/>��� 
    /// </summary>
    /// <param name="collider">���۴�����</param>
    /// <returns>��Ӧ��<see cref="Flower"/>��� </returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }

    private void Awake()
    {
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }

    /// <summary>
    /// ��ʼ״̬�£������<see cref="FlowerArea"/>�£�
    /// ���е�<see cref="Flower"/>��<see cref="flowerPlant"/>�����뵽<see cref="Flowers"/>��<see cref="flowerPlants"/>
    /// </summary>
    private void Start()
    {
        FindChildFlowers(transform);
    }

    /// <summary>
    /// ����һ������transform�µ����е�Flower��FlowerPlants
    /// </summary>
    /// <param name="transform">��Ҫ���ĸ��任</param>
    private void FindChildFlowers(Transform parent)
    {
        //�ݹ���ֹ����1���ҵ�һ��û��Flower�Ļ���
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.CompareTag("FlowerPlant"))
            {
                flowerPlants.Add(child.gameObject);  //�ҵ�һ�ػ���,���뵽�����б���
                FindChildFlowers(child);     //�ݹ����
            }
            else
            {
                Flower flower = child.GetComponent<Flower>();   //���ǻ��أ���Ӧ�ò��һ�
                if (flower != null)
                {   //�ݹ���ֹ����2���ҵ�һ�仨��������Ϊ������Ĭ�ϲ���Ƕ�׻�
                    Flowers.Add(flower);
                    //�����۴������ӵ�Collider->Flower�ֵ���
                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);
                }
                else
                {
                    FindChildFlowers(child);//Ҳû�л���Ӧ�õݹ����
                }

            }
        }
    }
}
