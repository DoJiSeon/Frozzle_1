using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class CharacterMovement : MonoBehaviour // 캐릭터 이동 및 애니메이션에 쓰이는 스크립트
{
    public Grid grid;
    public Tilemap map; // 타일맵 변수
    MouseInput mouseInput;  // 마우스 인풋 클래스 변수
    [SerializeField] private float movementSpeed; // 움직이는 스피드
    public Vector3 destination; // 마우스 클릭한 위치

    public bool is_map, is_move; // 맵 안에 있는가, 움직이는 가
    public bool is_walk, flip_check, is_right; // 걷고있냐, 애니메이션 반전 할거냐, 오른쪽 방향을 보고있냐
    public Animator animator; // 애니메이션 위한 애니메이터 변수
    public SpriteRenderer rend; // 스프라이트 변수
    Vector3 mousePos, transPos, targetPos, targetPos_rot; // 타겟의 방향 담을 변수

    public Vector3Int currentPos;
    public Tilemap tilemap;
    public GameObject player;

    public bool walkSound = false;

    public bool up_spell_check, right_spell_check, can_spell;

    public ParticleSystem particle;

    public static CharacterMovement _instance;
    public static CharacterMovement Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType(typeof(CharacterMovement)) as CharacterMovement;
                if (_instance == null)
                    Debug.Log("no singleton obj");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        mouseInput = new MouseInput(); // MouseInput 스크립트에서 가져와 객체 생성
    }
    private void OnEnable() //스크립트가 활성화 될때
    {
        mouseInput.Enable(); // 마우스 인풋 활성화
    }
    private void OnDisable() // 스크립트가 비활성화 될때
    {
        mouseInput.Disable(); // 마우스 인풋 비활성화
    }
    void Start()
    {
        destination = transform.position; // 현재 게임 오브젝트의 포지션 넣어줌
        mouseInput.Mouse.MouseClick.performed += _ => MouseClick(); // 아마 마우스 클릭 메소드와 연결해주는 것인듯?

        animator = GetComponent<Animator>(); // 현재 게임 오브젝트의 애니메이터 컴포넌트를 가져온다.
        rend = GetComponent<SpriteRenderer>(); // 현재 게임 오브젝트의 스프라이트렌더러 컴포넌트를 가져온다.
        flip_check = false; // 좌우 반전 할거냐?
        is_right = false; // 오른쪽 방향을 보고있냐?
        up_spell_check = false;
        right_spell_check = false;
        can_spell = false;

        tilemap = GetComponent<Tilemap>();
        player = GameObject.Find("player");
        particle = GetComponentInChildren<ParticleSystem>();
    }

    private void MouseClick()
    {
        Vector2 mousePosition = mouseInput.Mouse.MousePosition.ReadValue<Vector2>(); // 벡터 값으로 마우스 위치의 값을 받아온다.
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition); // 카메라 기준으로 마우스 포지션 값 바꿔주기
        Vector3Int gridPosition = map.WorldToCell(mousePosition); // 그리드 상의 포지션으로 가져오기
        if (map.HasTile(gridPosition)) // 그리드 포지션에 타일이 있다면?
        {
            destination = mousePosition; // 위치 값에 마우스 포지션 값 넣어주기
            // Debug.Log(destination);
        }
        CastRay();
    }
    void CastRay()
    {
        int layermask = 1 << LayerMask.NameToLayer("load");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity);
        // Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity,layermask)
        RaycastHit2D hit2 = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, LayerMask.GetMask("load"));
        if (hit2.collider != null) // hit.collider != null
        {
            Debug.Log(hit.collider.gameObject.tag);
            if (hit.collider.gameObject.tag == "map")
            {
                GameObject touchObj = hit2.transform.gameObject;
                Vector3 touchpos = touchObj.transform.position;
                destination = touchpos;
            }

        }
    }


    void Update()
    {
        //currentPos = tilemap.WorldToCell(player.transform.position);
        //Debug.Log(currentPos);

        if (Vector3.Distance(transform.position, destination) > 0.1f) // 마우스 위치와 현재 위치 사이의 거리가 0.1보다 크다면
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, movementSpeed * Time.deltaTime); // 그 위치로 이동
            find(); // 방향 관련 애니메이션 지시 메소드
            if (animator.GetBool("infall") == true)
            {
                animator.SetBool("infallmoving", true);

            }
        }
        else // 캐릭터의 위치가 마우스 위치와 같다면
        {
            is_walk = true; // 한번 걷고 있따 트루 해주고
            stop_walking(); // 메소드 한번 실행해주고
            is_walk = false; // 걷고있다 변수를 폴스로 바꿔주기
            if (flip_check == true) // 반대로 좌우반전 해줘야하는 지? 트루라면
            {
                rend.flipX = true; // 좌우반전해주고
                is_move = false; // 움직이기 중단
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                animator.SetTrigger("spell_t");
                spell();
            }
            animator.SetBool("infallmoving", false);
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "top1")
        {
            Debug.Log("탑과 부딫혔다 으앙 ><");
            PlayerManager.Instance.check_collide_top();
        }
        //else if (collision.gameObject.name == "paper1")
        //{
        //    PlayerPrefs.SetInt("clear_stage", 1);
        //    SceneManager.LoadScene("tutorial");
        //    //int clear_stage = PlayerPrefs.GetInt("clear_stage");
        //    //Debug.Log(clear_stage);
        //}
        //else if (collision.gameObject.name == "paper2")
        //{
        //    PlayerPrefs.SetInt("clear_stage", 2);
        //    SceneManager.LoadScene("tutorial");

        //}
        //else if (collision.gameObject.name == "paper3")
        //{
        //    PlayerPrefs.SetInt("clear_stage", 3);
        //    SceneManager.LoadScene("tutorial");

        //}
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.name == "top1")
        {
            Debug.Log("탑에서 나갈랭 ><");
            PlayerManager.Instance.check_collide_top();
        }
    }

    void stop_walking()
    {
        animator.SetBool("walk", false); // 애니메이터의 walk 변수를 폴스로 바꿔주기
    }

    void find()
    {
        targetPos_rot = new Vector3(destination.x, destination.y, 0); // 마우스 x,y 좌표 값을 타겟 포스 변수에 넣어주기, z값은 2d 니까 0!
        Vector3 dir_rot = targetPos_rot - transform.position; // 마우스에서 현재 포지션 값을 빼서 위치 값 구하기, 포지션엔 위치, 방향 등 다양한 정보 담고 있어
        dir_rot.y = 0;  // 이걸 왜했더라 x 값만 필요했나봐요~~^^
        Vector3 player_pos = new Vector3(transform.position.x, transform.position.y, 0); // 핸재 포지션의 값을 넣어주기

        if (targetPos_rot.y > player_pos.y) // 만약에 타겟의 y 값이 플레이어의 y 값보다 크다면? = 마우스 위치가 위쪽이라면?
        {
            if (targetPos_rot.x > player_pos.x) // 만약 마우스 위치가 오른쪽이라면?
            {
                rend.flipX = false; // 이미 오른쪽 애니메이터를 넣어둬서 뒤집을 필요가 없기 때문에 폴스

                animator.SetBool("walk", true);
                animator.SetBool("right", true);
                animator.SetBool("up", true);
                is_right = true;
                flip_check = false;
            }
            else if (targetPos_rot.x < player_pos.x) // 만약 위쪽이고 왼쪽쯤에 마우스 클릭이 있었떠라면?
            {
                rend.flipX = true; // 좌우반전 하기, 애니메이션을 오른쪽 방향만 넣어놨기 때문에 왼쪽 방향을 보려면 좌우반전 true 해야함
                animator.SetBool("walk", true);
                animator.SetBool("right", false);
                animator.SetBool("up", true);
                is_right = false;
                flip_check = true;
            }
        }
        else if (targetPos_rot.y < player_pos.y) // y축이 아래에 있다면, 마우스가 아래쪽에 클릭 됐다면?
        {
            if (targetPos_rot.x > player_pos.x) // 오른쪽 방향이 클릭되있다면?
            {

                rend.flipX = true; // 현재 아래쪽 모션은 왼쪽 방향으로 가는거가 기본으로 되어있어서 오른쪽 방향 보려면 뒤집기 true 해야함
                animator.SetBool("walk", true);
                animator.SetBool("right", true);
                animator.SetBool("up", false);
                is_right = false;
                flip_check = true;
            }
            else if (targetPos_rot.x < player_pos.x) // 왼쪽 방향 클릭 되었다면?
            {

                rend.flipX = false;
                animator.SetBool("walk", true);
                animator.SetBool("right", false);
                animator.SetBool("up", false);
                is_right = true;
                flip_check = false;
            }
        }
    }

    void spell()
    {
        targetPos_rot = new Vector3(destination.x, destination.y, 0); // 마우스 x,y 좌표 값을 타겟 포스 변수에 넣어주기, z값은 2d 니까 0!
        Vector3 dir_rot = targetPos_rot - transform.position; // 마우스에서 현재 포지션 값을 빼서 위치 값 구하기, 포지션엔 위치, 방향 등 다양한 정보 담고 있어
        dir_rot.y = 0;  // 이걸 왜했더라 x 값만 필요했나봐요~~^^
        Vector3 player_pos = new Vector3(transform.position.x, transform.position.y, 0); // 핸재 포지션의 값을 넣어주기

        if (animator.GetBool("up") == true) // 만약에 타겟의 y 값이 플레이어의 y 값보다 크다면? = 마우스 위치가 위쪽이라면?
        {
            if (animator.GetBool("right") == true) // 만약 마우스 위치가 오른쪽이라면?
            {
                rend.flipX = false; // 이미 오른쪽 애니메이터를 넣어둬서 뒤집을 필요가 없기 때문에 폴스

                animator.SetBool("walk", false);
                animator.SetBool("right", true);
                animator.SetBool("up", true);
                animator.SetTrigger("spell_t");
                //animator.SetBool("spell", true);
                is_right = true;
                flip_check = false;
                StartCoroutine("Particle");
            }
            else if (animator.GetBool("right") == false) // 만약 위쪽이고 왼쪽쯤에 마우스 클릭이 있었떠라면?
            {
                rend.flipX = true; // 좌우반전 하기, 애니메이션을 오른쪽 방향만 넣어놨기 때문에 왼쪽 방향을 보려면 좌우반전 true 해야함
                animator.SetBool("walk", false);
                animator.SetBool("right", false);
                animator.SetBool("up", true);
                animator.SetTrigger("spell_t");
                //animator.SetBool("spell", true);
                is_right = false;
                flip_check = true;
                StartCoroutine("Particle");
            }
        }
        else if (animator.GetBool("up") == false) // y축이 아래에 있다면, 마우스가 아래쪽에 클릭 됐다면?
        {
            if (animator.GetBool("right") == true) // 오른쪽 방향이 클릭되있다면?
            {

                rend.flipX = true; // 현재 아래쪽 모션은 왼쪽 방향으로 가는거가 기본으로 되어있어서 오른쪽 방향 보려면 뒤집기 true 해야함
                animator.SetBool("walk", false);
                animator.SetBool("right", true);
                animator.SetBool("up", false);
                animator.SetTrigger("spell_t");
                //animator.SetBool("spell", true);
                is_right = false;
                flip_check = true;
                StartCoroutine("Particle");
            }
            else if (animator.GetBool("right") == false) // 왼쪽 방향 클릭 되었다면?
            {

                rend.flipX = false;
                animator.SetBool("walk", false);
                animator.SetBool("right", false);
                animator.SetBool("up", false);
                animator.SetTrigger("spell_t");
                //animator.SetBool("spell", true);
                is_right = true;
                flip_check = false;
                StartCoroutine("Particle");
            }
        }
    }

    public IEnumerator Particle()
    {
        particle.Play();
        yield return new WaitForSeconds(particle.main.startLifetime.constantMax);
    }
}