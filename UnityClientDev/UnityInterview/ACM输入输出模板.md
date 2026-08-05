# ACM 算法题输入输出处理模板

> 速查用，做题前看一遍唤醒记忆。
> 适用于在线评测系统（牛客、POJ、Codeforces 等）和面试笔试平台。

---

## 目录

- [一、完整程序框架（C++）](#一完整程序框架c)
  - [1.1 万能模板（含常用头文件 + 宏 + 调试）](#11-万能模板含常用头文件--宏--调试)
  - [1.2 必须包含的头文件说明](#12-必须包含的头文件说明)
  - [1.3 常用类型别名与常量](#13-常用类型别名与常量)
  - [1.4 调试用宏](#14-调试用宏)
- [二、完整程序框架（C#）](#二完整程序框架c)
  - [2.1 标准模板](#21-标准模板)
  - [2.2 自定义快速读取器（大数据量用）](#22-自定义快速读取器大数据量用)
- [三、基础输入输出（C#）](#三基础输入输出c)
  - [3.1 读取整数](#31-读取整数)
  - [3.2 读取一行整数到数组](#32-读取一行整数到数组)
  - [3.3 连续读取直到 EOF](#33-连续读取直到-eof)
  - [3.4 指定行数 + 每行解析](#34-指定行数--每行解析)
  - [3.5 读取多行字符串](#35-读取多行字符串)
  - [3.6 读取矩阵](#36-读取矩阵)
  - [3.7 使用 StringBuilder 输出](#37-使用-stringbuilder-输出)
  - [3.8 浮点数输出精度控制](#38-浮点数输出精度控制)
- [四、基础输入输出（C++）](#四基础输入输出c)
  - [4.1 读取整数](#41-读取整数)
  - [4.2 读取一行整数到 vector](#42-读取一行整数到-vector)
  - [4.3 连续读取直到 EOF](#43-连续读取直到-eof)
  - [4.4 指定行数 + 每行解析](#44-指定行数--每行解析)
  - [4.5 读取多行字符串](#45-读取多行字符串)
  - [4.6 读取矩阵](#46-读取矩阵)
  - [4.7 浮点数输出精度控制](#47-浮点数输出精度控制)
- [五、常用算法骨架](#五常用算法骨架)
  - [5.1 二分查找](#51-二分查找)
  - [5.2 图：邻接表建图](#52-图邻接表建图)
  - [5.3 图：邻接矩阵建图](#53-图邻接矩阵建图)
  - [5.4 树：边列表建树（无根树）](#54-树边列表建树无根树)
  - [5.5 并查集 DSU](#55-并查集-dsu)
  - [5.6 前缀和与差分](#56-前缀和与差分)
  - [5.7 排序 + 自定义比较器](#57-排序--自定义比较器)
  - [5.8 模运算（防溢出）](#58-模运算防溢出)
  - [5.9 组合数 C(n,k) 预处理](#59-组合数-cnk-预处理)
  - [5.10 滑动窗口](#510-滑动窗口)
  - [5.11 差分数组](#511-差分数组)
  - [5.12 二维数组遍历](#512-二维数组遍历)
  - [5.13 数组双指针](#513-数组双指针)
  - [5.14 链表操作](#514-链表操作)
  - [5.15 二叉树遍历](#515-二叉树遍历)
  - [5.16 BFS 广度优先搜索](#516-bfs-广度优先搜索)
  - [5.17 回溯算法](#517-回溯算法)
  - [5.18 拓扑排序](#518-拓扑排序)
  - [5.19 Dijkstra 最短路径](#519-dijkstra-最短路径)
  - [5.20 单调栈](#520-单调栈)
  - [5.21 单调队列](#521-单调队列)
  - [5.22 字典树 Trie](#522-字典树-trie)
  - [5.23 动态规划框架](#523-动态规划框架)
- [六、常见输入格式速查表](#六常见输入格式速查表)
- [七、最容易踩的坑](#七最容易踩的坑)

---

## 一、完整程序框架（C++）

### 1.1 万能模板（含常用头文件 + 宏 + 调试）

```cpp
#include <bits/stdc++.h>          // 万能头（部分 OJ 不支持，见 1.2）
using namespace std;

using ll = long long;
using ull = unsigned long long;
using pii = pair<int, int>;
using vi = vector<int>;
using vll = vector<ll>;

const int INF = 0x3f3f3f3f;              // 常用大数（防溢出）
const ll INFLL = 0x3f3f3f3f3f3f3f3fLL;
const int MOD = 1e9 + 7;                 // 常用模数

// 最大最小值
template<typename T> T Max(T a, T b) { return a > b ? a : b; }
template<typename T> T Min(T a, T b) { return a < b ? a : b; }

// 调试开关（提交前关掉）
#define DEBUG
#ifdef DEBUG
    #define dbg(x) cerr << #x << " = " << x << endl
    #define dbg2(x,y) cerr << #x << " = " << x << ", " << #y << " = " << y << endl
    #define dbgarr(a,n) { cerr << #a << " = "; for(int _=0; _<n; _++) cerr << a[_] << " \n"[_==n-1]; }
#else
    #define dbg(x)
    #define dbg2(x,y)
    #define dbgarr(a,n)
#endif

void solve() {
    // ===== 在这里写主逻辑 =====
}

int main() {
    ios::sync_with_stdio(false);
    cin.tie(nullptr);

    int T = 1;
    // cin >> T;               // 有多组测试数据时取消注释
    while (T--) {
        solve();
    }

    return 0;
}
```

### 1.2 必须包含的头文件说明

> `#include <bits/stdc++.h>` 是 GNU C++ 的万能头，**Codeforces / POJ 支持**，但 **Visual Studio / 部分 OJ 不支持**。
> 不支持的场合，手动包含以下常用头：

| 头文件 | 用途 |
|--------|------|
| `#include <iostream>` | cin/cout |
| `#include <cstdio>` | scanf/printf（备用） |
| `#include <algorithm>` | sort/min/max/lower_bound/unique |
| `#include <vector>` | 动态数组 |
| `#include <queue>` | queue / priority_queue |
| `#include <stack>` | stack |
| `#include <deque>` | deque（滑动窗口常用） |
| `#include <set>` / `<map>` | 有序集合/映射 |
| `#include <unordered_set>` / `<unordered_map>` | 哈希集合/映射（C++11+） |
| `#include <string>` | string + getline |
| `#include <sstream>` | stringstream 字符串解析 |
| `#include <cmath>` | abs / sqrt / pow / ceil / floor |
| `#include <climits>` | INT_MAX / LLONG_MAX |
| `#include <cstring>` | memset / memcpy |
| `#include <numeric>` | accumulate / gcd（C++17+） |
| `#include <functional>` | greater\<int\> / less\<int\> |
| `#include <bitset>` | 位集运算 |

```cpp
// 安全的"手动万能头"方案
#include <iostream>
#include <algorithm>
#include <vector>
#include <queue>
#include <stack>
#include <set>
#include <map>
#include <unordered_set>
#include <unordered_map>
#include <string>
#include <sstream>
#include <cmath>
#include <climits>
#include <cstring>
#include <numeric>
#include <functional>
using namespace std;
```

### 1.3 常用类型别名与常量

```cpp
using ll = long long;
using ull = unsigned long long;
using ld = long double;          // 高精度浮点

using pii = pair<int, int>;
using pll = pair<ll, ll>;
using vi = vector<int>;
using vll = vector<ll>;
using vvi = vector<vector<int>>;
using vs = vector<string>;

const int INF = 0x3f3f3f3f;              // 约 1e9，防溢出（INT_MAX/2）
const ll INFLL = 0x3f3f3f3f3f3f3f3fLL;
const int MOD = 1e9 + 7;
const double EPS = 1e-9;                 // 浮点精度比较
```

> **为什么用 `0x3f3f3f3f` 而不用 `INT_MAX`？**
> `INT_MAX` 加一个正数会溢出变成负数；`0x3f3f3f3f` 是两个 `0x3f3f3f3f` 相加仍不溢出（约 2e9），常用于 `memset(arr, 0x3f, sizeof(arr))`。

### 1.4 调试用宏

```cpp
// 提交前把 #define DEBUG 注释掉，所有 dbg 语句就不生成代码
#define DEBUG

#ifdef DEBUG
    #define dbg(x) cerr << #x << " = " << x << endl
    #define dbg2(x,y) cerr << #x << " = " << x << ", " << #y << " = " << y << endl
    #define dbgr(v) { cerr << #v << " = "; for(auto x:v) cerr << x << " "; cerr << endl; }
    #define dbgp(p) cerr << #p << " = (" << p.first << "," << p.second << ")" << endl
    #define dbgarr(a,n) { cerr << #a << " = "; for(int _=0; _<(n); _++) cerr << a[_] << " \n"[_==(n)-1]; }
#else
    #define dbg(x)
    #define dbg2(x,y)
    #define dbgr(v)
    #define dbgp(p)
    #define dbgarr(a,n)
#endif

// 使用示例
void solve() {
    int n = 5;
    dbg(n);                    // cerr → n = 5
    vi arr = {1, 2, 3, 4, 5};
    dbgr(arr);                 // cerr → arr = 1 2 3 4 5
}
```

---

## 二、完整程序框架（C#）

### 2.1 标准模板

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Program
{
    static void Solve()
    {
        // ===== 在这里写主逻辑 =====
    }

    static void Main()
    {
        // int T = int.Parse(Console.ReadLine());
        // while (T-- > 0) Solve();
        Solve();
    }
}
```

**多组测试数据版：**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Program
{
    static void Main()
    {
        // 方式 1：第一行 T，每组一行
        int T = int.Parse(Console.ReadLine());
        StringBuilder sb = new StringBuilder();
        while (T-- > 0)
        {
            string[] parts = Console.ReadLine().Split();
            int a = int.Parse(parts[0]);
            int b = int.Parse(parts[1]);
            sb.AppendLine((a + b).ToString());
        }
        Console.Write(sb.ToString());

        // 方式 2：读到 EOF
        // string line;
        // while ((line = Console.ReadLine()) != null)
        // {
        //     if (string.IsNullOrEmpty(line)) continue;
        //     // ...
        // }
    }
}
```

### 2.2 自定义快速读取器（大数据量用）

> ACM 输入超过 **10^5 行**时，`Console.ReadLine().Split().Select(int.Parse)` 的 LINQ 开销很大。
> 用下面的 `FastReader`，速度能提升 3-5 倍。

```csharp
using System;
using System.IO;

public class FastReader
{
    private readonly StreamReader _reader;
    private string[] _tokens;
    private int _pos;

    public FastReader()
    {
        _reader = new StreamReader(Console.OpenStandardInput());
        _tokens = new string[0];
        _pos = 0;
    }

    private string ReadLine()
    {
        return _reader.ReadLine();
    }

    public string Next()
    {
        while (_pos >= _tokens.Length)
        {
            string line = ReadLine();
            if (line == null) return null;  // EOF
            // 按空格和换行符分割，移除空条目
            _tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            _pos = 0;
        }
        return _tokens[_pos++];
    }

    public int NextInt()
    {
        return int.Parse(Next());
    }

    public long NextLong()
    {
        return long.Parse(Next());
    }

    public double NextDouble()
    {
        return double.Parse(Next());
    }
}

// 使用示例
class Program
{
    static void Main()
    {
        FastReader fr = new FastReader();
        int n = fr.NextInt();
        int[] arr = new int[n];
        for (int i = 0; i < n; i++)
            arr[i] = fr.NextInt();
        // ...
    }
}
```

---

## 三、基础输入输出（C#）

### 3.1 读取整数

```csharp
// 读 1 个整数
int n = int.Parse(Console.ReadLine());

// 读 2 个整数
string[] parts = Console.ReadLine().Split();
int a = int.Parse(parts[0]);
int b = int.Parse(parts[1]);
```

### 3.2 读取一行整数到数组

```csharp
// 一行空格分隔的整数 → 数组
int[] arr = Console.ReadLine().Split().Select(int.Parse).ToArray();

// 或手动解析（更直观）
string[] parts = Console.ReadLine().Split();
int[] arr = new int[parts.Length];
for (int i = 0; i < parts.Length; i++)
    arr[i] = int.Parse(parts[i]);
```

### 3.3 连续读取直到 EOF

```csharp
// 不知道有多少行，读到没有为止
string line;
while ((line = Console.ReadLine()) != null)
{
    if (string.IsNullOrEmpty(line)) continue;  // 跳过空行
    string[] parts = line.Split();
    int a = int.Parse(parts[0]);
    int b = int.Parse(parts[1]);
    Console.WriteLine(a + b);
}
```

### 3.4 指定行数 + 每行解析

```csharp
// 输入：
// 3
// 1 2
// 3 4
// 5 6

int n = int.Parse(Console.ReadLine());
for (int i = 0; i < n; i++)
{
    string[] parts = Console.ReadLine().Split();
    int a = int.Parse(parts[0]);
    int b = int.Parse(parts[1]);
    Console.WriteLine(a + b);
}
```

### 3.5 读取多行字符串

```csharp
// 第一行 n，后面 n 行字符串
int n = int.Parse(Console.ReadLine());
string[] lines = new string[n];
for (int i = 0; i < n; i++)
    lines[i] = Console.ReadLine();
```

### 3.6 读取矩阵

```csharp
// 输入：
// 3 4
// 1 2 3 4
// 5 6 7 8
// 9 1 2 3

string[] first = Console.ReadLine().Split();
int rows = int.Parse(first[0]);
int cols = int.Parse(first[1]);

int[,] matrix = new int[rows, cols];
for (int i = 0; i < rows; i++)
{
    int[] row = Console.ReadLine().Split().Select(int.Parse).ToArray();
    for (int j = 0; j < cols; j++)
        matrix[i, j] = row[j];
}
```

### 3.7 使用 StringBuilder 输出

> 大量输出时（几千行），用 `Console.WriteLine` 逐个输出很慢。
> 统一收集到 `StringBuilder` 最后一次性输出。

```csharp
using System.Text;

StringBuilder sb = new StringBuilder();

for (int i = 0; i < n; i++)
{
    int result = ComputeSomething();
    if (i > 0) sb.AppendLine();  // 两组结果之间空行（某些题目要求）
    sb.Append(result);
}

// 最后一次性输出
Console.Write(sb.ToString());
```

### 3.8 浮点数输出精度控制

```csharp
// 保留 6 位小数
double d = 3.1415926535;
Console.WriteLine(d.ToString("F6"));     // 3.141593
Console.WriteLine($"{d:F6}");            // 3.141593（字符串插值）

// 保留指定位数
double pi = Math.PI;
Console.WriteLine($"{pi:F2}");           // 3.14
Console.WriteLine($"{pi:F10}");          // 3.1415926536
```

---

## 四、基础输入输出（C++）

### 4.1 读取整数

```cpp
// 读 1 个整数
int n;
cin >> n;

// 读 2 个整数
int a, b;
cin >> a >> b;
```

### 4.2 读取一行整数到 vector

```cpp
// 方式 1：用 cin.peek 检测换行
vector<int> arr;
int x;
while (cin.peek() != '\n' && cin >> x) {
    arr.push_back(x);
}

// 方式 2：先读整行再解析（更可控）
string line;
getline(cin, line);
stringstream ss(line);
vector<int> arr;
int x;
while (ss >> x) {
    arr.push_back(x);
}
```

### 4.3 连续读取直到 EOF

```cpp
// 方式 1：cin >> 直接判断
int a, b;
while (cin >> a >> b) {
    cout << a + b << endl;
}

// 方式 2：读行判断
string line;
while (getline(cin, line)) {
    if (line.empty()) continue;
    // 处理 line...
}
```

### 4.4 指定行数 + 每行解析

```cpp
int n;
cin >> n;
while (n--) {
    int a, b;
    cin >> a >> b;
    cout << a + b << "\n";
}
```

### 4.5 读取多行字符串

```cpp
int n;
cin >> n;
cin.ignore();  // ⚠ 吃掉第一行末尾的换行符！

vector<string> lines(n);
for (int i = 0; i < n; i++) {
    getline(cin, lines[i]);
}
```

### 4.6 读取矩阵

```cpp
int rows, cols;
cin >> rows >> cols;

vector<vector<int>> matrix(rows, vector<int>(cols));
for (int i = 0; i < rows; i++)
    for (int j = 0; j < cols; j++)
        cin >> matrix[i][j];
```

### 4.7 浮点数输出精度控制

```cpp
#include <iomanip>    // 需要这个头文件

double d = 3.1415926535;

// 保留 6 位小数
cout << fixed << setprecision(6) << d << endl;   // 3.141593

// 保留 2 位
cout << fixed << setprecision(2) << d << endl;   // 3.14

// 取消 fixed 后是科学计数法
cout << setprecision(6) << d << endl;            // 3.14159（默认不补零）
```

---

## 五、常用算法骨架

### 5.1 二分查找

```cpp
// 在有序数组 arr 中找 target 的第一次出现位置（lower_bound）
int lowerBound(const vector<int>& arr, int target) {
    int left = 0, right = arr.size();  // 左闭右开
    while (left < right) {
        int mid = left + (right - left) / 2;
        if (arr[mid] >= target)
            right = mid;
        else
            left = mid + 1;
    }
    return left;  // 第一个 >= target 的位置
}

// 在有序数组 arr 中找 target 的最后一次出现位置（upper_bound - 1）
int upperBound(const vector<int>& arr, int target) {
    int left = 0, right = arr.size();
    while (left < right) {
        int mid = left + (right - left) / 2;
        if (arr[mid] > target)
            right = mid;
        else
            left = mid + 1;
    }
    return left - 1;  // 最后一个 <= target 的位置
}

// 浮点数二分（求平方根示例）
double sqrtBinary(double x) {
    double left = 0, right = x;
    for (int i = 0; i < 100; i++) {          // 迭代 100 次保证精度
        double mid = (left + right) / 2;
        if (mid * mid >= x)
            right = mid;
        else
            left = mid;
    }
    return left;
}
```

```csharp
// C# 二分查找（手写）
int LowerBound(int[] arr, int target) {
    int left = 0, right = arr.Length;  // 左闭右开
    while (left < right) {
        int mid = left + (right - left) / 2;
        if (arr[mid] >= target)
            right = mid;
        else
            left = mid + 1;
    }
    return left;
}
// 或直接用内置：
// int idx = Array.BinarySearch(arr, target);   // 找到返回索引，没找到返回负数
```

### 5.2 图：邻接表建图

```cpp
// 输入格式：
// n m
// u1 v1
// u2 v2
// ...

int n, m;
cin >> n >> m;
vector<vector<int>> g(n + 1);   // 1-indexed

for (int i = 0; i < m; i++) {
    int u, v;
    cin >> u >> v;
    g[u].push_back(v);
    g[v].push_back(u);   // 无向图加这句
}
```

```csharp
// C# 邻接表
int n = int.Parse(parts[0]), m = int.Parse(parts[1]);
List<int>[] g = new List<int>[n + 1];
for (int i = 1; i <= n; i++) g[i] = new List<int>();

for (int i = 0; i < m; i++) {
    var p = Console.ReadLine().Split();
    int u = int.Parse(p[0]), v = int.Parse(p[1]);
    g[u].Add(v);
    g[v].Add(u);   // 无向图
}
```

### 5.3 图：邻接矩阵建图

```cpp
// 稠密图（n ≤ 2000 时可用）
int n, m;
cin >> n >> m;
vector<vector<int>> mat(n + 1, vector<int>(n + 1, 0));

for (int i = 0; i < m; i++) {
    int u, v;
    cin >> u >> v;
    mat[u][v] = 1;      // 无权图
    mat[v][u] = 1;      // 无向图
    // mat[u][v] = w;   // 带权图
}
```

### 5.4 树：边列表建树（无根树）

```cpp
// 输入：n-1 条边
int n;
cin >> n;
vector<vector<int>> tree(n + 1);
for (int i = 0; i < n - 1; i++) {
    int u, v;
    cin >> u >> v;
    tree[u].push_back(v);
    tree[v].push_back(u);
}

// DFS 遍历树（从根节点 1 开始）
vector<bool> visited(n + 1, false);

void dfs(int u) {
    visited[u] = true;
    for (int v : tree[u]) {
        if (!visited[v]) {
            // 处理边 u-v
            dfs(v);
        }
    }
}
```

### 5.5 并查集 DSU

```cpp
struct DSU {
    vector<int> parent, rank;

    DSU(int n) {
        parent.resize(n + 1);
        rank.resize(n + 1, 0);
        for (int i = 1; i <= n; i++) parent[i] = i;
    }

    int find(int x) {
        // 路径压缩
        if (parent[x] != x)
            parent[x] = find(parent[x]);
        return parent[x];
    }

    void unite(int x, int y) {
        int px = find(x), py = find(y);
        if (px == py) return;
        // 按秩合并
        if (rank[px] < rank[py]) swap(px, py);
        parent[py] = px;
        if (rank[px] == rank[py]) rank[px]++;
    }

    bool same(int x, int y) {
        return find(x) == find(y);
    }
};

// 使用
DSU dsu(n);
dsu.unite(u, v);
bool connected = dsu.same(a, b);
```

```csharp
class DSU {
    int[] parent, rank;

    public DSU(int n) {
        parent = new int[n + 1];
        rank = new int[n + 1];
        for (int i = 1; i <= n; i++) parent[i] = i;
    }

    public int Find(int x) {
        if (parent[x] != x)
            parent[x] = Find(parent[x]);
        return parent[x];
    }

    public void Unite(int x, int y) {
        int px = Find(x), py = Find(y);
        if (px == py) return;
        if (rank[px] < rank[py]) { int t = px; px = py; py = t; }
        parent[py] = px;
        if (rank[px] == rank[py]) rank[px]++;
    }

    public bool Same(int x, int y) => Find(x) == Find(y);
}
```

### 5.6 前缀和与差分

```cpp
// 一维前缀和
vector<int> arr(n), pref(n + 1, 0);
for (int i = 0; i < n; i++) {
    cin >> arr[i];
    pref[i + 1] = pref[i] + arr[i];    // pref[i] = arr[0..i-1] 的和
}
// 查询 [l, r] 区间和 O(1)
int sum = pref[r + 1] - pref[l];

// 二维前缀和
vector<vector<int>> mat(rows + 1, vector<int>(cols + 1, 0));
for (int i = 1; i <= rows; i++)
    for (int j = 1; j <= cols; j++) {
        cin >> mat[i][j];
        mat[i][j] += mat[i-1][j] + mat[i][j-1] - mat[i-1][j-1];
    }
// 查询 (r1,c1) 到 (r2,c2) 子矩阵和
int sum = mat[r2][c2] - mat[r1-1][c2] - mat[r2][c1-1] + mat[r1-1][c1-1];
```

```csharp
// C# 一维前缀和
int[] arr = Console.ReadLine().Split().Select(int.Parse).ToArray();
int[] pref = new int[n + 1];
for (int i = 0; i < n; i++)
    pref[i + 1] = pref[i] + arr[i];
```

### 5.7 排序 + 自定义比较器

```cpp
// C++：默认升序
sort(arr.begin(), arr.end());

// 降序
sort(arr.begin(), arr.end(), greater<int>());

// 自定义比较（按 pair 的 second 降序）
sort(vp.begin(), vp.end(), [](const pii& a, const pii& b) {
    if (a.second != b.second) return a.second > b.second;
    return a.first < b.first;
});
```

```csharp
// C#：默认升序
Array.Sort(arr);

// 降序
Array.Sort(arr, (a, b) => b.CompareTo(a));

// 自定义比较（按 Item2 降序，Item1 升序）
var list = new List<(int, int)>();
list.Sort((a, b) => {
    int cmp = b.Item2.CompareTo(a.Item2);  // second 降序
    if (cmp != 0) return cmp;
    return a.Item1.CompareTo(b.Item1);     // first 升序
});

// 或使用 LINQ
var sorted = list.OrderByDescending(x => x.Item2).ThenBy(x => x.Item1).ToList();
```

### 5.8 模运算（防溢出）

```cpp
const int MOD = 1e9 + 7;

ll modAdd(ll a, ll b) { return (a + b) % MOD; }
ll modSub(ll a, ll b) { return (a - b + MOD) % MOD; }
ll modMul(ll a, ll b) { return (a * b) % MOD; }

// 快速幂
ll modPow(ll base, ll exp) {
    ll res = 1;
    while (exp > 0) {
        if (exp & 1) res = (res * base) % MOD;
        base = (base * base) % MOD;
        exp >>= 1;
    }
    return res;
}
```

```csharp
const int MOD = 1000000007;

long ModAdd(long a, long b) => (a + b) % MOD;
long ModSub(long a, long b) => (a - b + MOD) % MOD;
long ModMul(long a, long b) => (a * b) % MOD;

long ModPow(long base, long exp) {
    long res = 1;
    while (exp > 0) {
        if ((exp & 1) == 1) res = (res * base) % MOD;
        base = (base * base) % MOD;
        exp >>= 1;
    }
    return res;
}
```

### 5.9 组合数 C(n,k) 预处理

> 大量查询组合数时，预处理阶乘和逆元，O(1) 查询。

```cpp
const int MAXN = 200000;   // 按题目上限改
ll fact[MAXN + 1], invFact[MAXN + 1];

ll modPow(ll base, ll exp) {
    ll res = 1;
    while (exp > 0) {
        if (exp & 1) res = (res * base) % MOD;
        base = (base * base) % MOD;
        exp >>= 1;
    }
    return res;
}

void initComb() {
    fact[0] = 1;
    for (int i = 1; i <= MAXN; i++)
        fact[i] = fact[i-1] * i % MOD;

    invFact[MAXN] = modPow(fact[MAXN], MOD - 2);  // 费马小定理求逆元
    for (int i = MAXN - 1; i >= 0; i--)
        invFact[i] = invFact[i+1] * (i+1) % MOD;
}

ll C(int n, int k) {
    if (k < 0 || k > n) return 0;
    return fact[n] * invFact[k] % MOD * invFact[n - k] % MOD;
}

// 使用
initComb();
cout << C(10, 3) << endl;  // 120
```

### 5.10 滑动窗口 Sliding Window

> **适用场景：** 子数组/子串的连续区间问题，时间复杂度 O(N)（每个元素进窗口一次、出窗口一次）。

```cpp
// ===== 可变滑动窗口（求最长/最短满足条件子数组） =====
// 框架：扩大右边界，不满足时收缩左边界
int slidingWindow(const vector<int>& nums, int target) {
    int left = 0, right = 0;
    int sum = 0, ans = 0;  // ans 根据题目调整（最大/最小长度等）
    int n = nums.size();

    while (right < n) {
        // 扩大窗口：加入 nums[right]
        sum += nums[right];
        right++;

        // 收缩窗口：当窗口不满足条件时，移动 left
        while (/* 窗口不满足条件 */) {
            // 记录答案（最短类问题在此位置记录）
            sum -= nums[left];
            left++;
        }

        // 记录答案（最长类问题在此位置记录）
        ans = max(ans, right - left);
    }
    return ans;
}

// ===== 固定滑动窗口（大小为 k） =====
vector<int> fixedWindow(const vector<int>& nums, int k) {
    vector<int> result;
    int sum = 0;
    for (int i = 0; i < nums.size(); i++) {
        sum += nums[i];                    // 加入新元素
        if (i >= k - 1) {                  // 窗口形成
            result.push_back(sum);
            sum -= nums[i - k + 1];        // 移除窗口左端
        }
    }
    return result;
}
```

```csharp
// 可变滑动窗口（字符串版本）
int SlidingWindow(string s) {
    int left = 0, right = 0, ans = 0;
    var freq = new Dictionary<char, int>();

    while (right < s.Length) {
        // 扩大窗口
        char c = s[right]; right++;
        if (!freq.ContainsKey(c)) freq[c] = 0;
        freq[c]++;

        // 不满足条件时收缩
        while (/* 需要收缩的条件 */) {
            char d = s[left]; left++;
            if (--freq[d] == 0) freq.Remove(d);
        }

        ans = Math.Max(ans, right - left);
    }
    return ans;
}

// 固定窗口：滑动窗口最大值（见 5.21 单调队列）
```

### 5.11 差分数组 Difference Array

> **适用场景：** 多次区间增减操作后，还原最终数组值。O(N) 完成多次区间更新。

```cpp
// 差分数组模板：对 [l, r] 区间加 val，最后还原
class Difference {
    vector<int> diff;  // diff[i] = arr[i] - arr[i-1]
public:
    Difference(const vector<int>& arr) {
        diff.resize(arr.size());
        diff[0] = arr[0];
        for (int i = 1; i < arr.size(); i++)
            diff[i] = arr[i] - arr[i - 1];
    }

    // 对区间 [l, r] 加 val（闭区间）
    void add(int l, int r, int val) {
        diff[l] += val;
        if (r + 1 < diff.size())
            diff[r + 1] -= val;
    }

    // 还原为原数组
    vector<int> restore() {
        vector<int> res(diff.size());
        res[0] = diff[0];
        for (int i = 1; i < diff.size(); i++)
            res[i] = res[i - 1] + diff[i];
        return res;
    }
};

// 使用示例
// vector<int> arr(n, 0);
// Difference df(arr);
// df.add(l, r, val);   // 对 [l, r] 加 val
// vector<int> result = df.restore();
```

```csharp
class Difference {
    int[] diff;

    public Difference(int[] arr) {
        diff = new int[arr.Length];
        diff[0] = arr[0];
        for (int i = 1; i < arr.Length; i++)
            diff[i] = arr[i] - arr[i - 1];
    }

    public void Add(int l, int r, int val) {
        diff[l] += val;
        if (r + 1 < diff.Length)
            diff[r + 1] -= val;
    }

    public int[] Restore() {
        var res = new int[diff.Length];
        res[0] = diff[0];
        for (int i = 1; i < diff.Length; i++)
            res[i] = res[i - 1] + diff[i];
        return res;
    }
}
```

### 5.12 二维数组遍历

> **适用场景：** 螺旋矩阵、矩阵旋转、二维网格 DFS（Flood Fill / 岛屿问题）。

```cpp
// ===== 螺旋遍历矩阵 =====
vector<int> spiralOrder(const vector<vector<int>>& matrix) {
    if (matrix.empty()) return {};
    int top = 0, bottom = matrix.size() - 1;
    int left = 0, right = matrix[0].size() - 1;
    vector<int> res;

    while (top <= bottom && left <= right) {
        // 从左到右
        for (int j = left; j <= right; j++) res.push_back(matrix[top][j]);
        top++;
        // 从上到下
        for (int i = top; i <= bottom; i++) res.push_back(matrix[i][right]);
        right--;
        if (top <= bottom)
            for (int j = right; j >= left; j--) res.push_back(matrix[bottom][j]);
        bottom--;
        if (left <= right)
            for (int i = bottom; i >= top; i--) res.push_back(matrix[i][left]);
        left++;
    }
    return res;
}

// ===== 顺时针旋转矩阵 90° =====
void rotate(vector<vector<int>>& matrix) {
    int n = matrix.size();
    // 1. 沿主对角线翻转
    for (int i = 0; i < n; i++)
        for (int j = i + 1; j < n; j++)
            swap(matrix[i][j], matrix[j][i]);
    // 2. 每行反转
    for (int i = 0; i < n; i++)
        reverse(matrix[i].begin(), matrix[i].end());
}

// ===== 二维网格 DFS（Flood Fill / 岛屿问题） =====
void dfs(vector<vector<char>>& grid, int i, int j) {
    int m = grid.size(), n = grid[0].size();
    if (i < 0 || j < 0 || i >= m || j >= n) return;
    if (grid[i][j] == '0') return;
    grid[i][j] = '0';  // 淹没（标记已访问）
    dfs(grid, i + 1, j);
    dfs(grid, i - 1, j);
    dfs(grid, i, j + 1);
    dfs(grid, i, j - 1);
}
// 使用：遍历网格，遇到 '1' 就 DFS 淹没一片，count++
```

```csharp
// 螺旋遍历矩阵
IList<int> SpiralOrder(int[][] matrix) {
    var res = new List<int>();
    if (matrix.Length == 0) return res;
    int top = 0, bottom = matrix.Length - 1;
    int left = 0, right = matrix[0].Length - 1;

    while (top <= bottom && left <= right) {
        for (int j = left; j <= right; j++) res.Add(matrix[top][j]);
        top++;
        for (int i = top; i <= bottom; i++) res.Add(matrix[i][right]);
        right--;
        if (top <= bottom)
            for (int j = right; j >= left; j--) res.Add(matrix[bottom][j]);
        bottom--;
        if (left <= right)
            for (int i = bottom; i >= top; i--) res.Add(matrix[i][left]);
        left++;
    }
    return res;
}

// 二维网格 DFS
void Flood(char[][] grid, int i, int j) {
    if (i < 0 || j < 0 || i >= grid.Length || j >= grid[0].Length) return;
    if (grid[i][j] == '0') return;
    grid[i][j] = '0';
    Flood(grid, i + 1, j); Flood(grid, i - 1, j);
    Flood(grid, i, j + 1); Flood(grid, i, j - 1);
}
```

### 5.13 数组双指针

> **适用场景：** 有序数组搜索、in-place 删除、nSum 问题。快慢指针 O(N)、左右指针 O(N)。

```cpp
// ===== 快慢指针：in-place 删除重复/特定元素 =====
int removeDuplicates(vector<int>& nums) {
    if (nums.empty()) return 0;
    int slow = 0, fast = 1;
    while (fast < nums.size()) {
        if (nums[fast] != nums[slow]) {
            slow++;
            nums[slow] = nums[fast];
        }
        fast++;
    }
    return slow + 1;  // 新长度
}

// ===== 左右指针：两数之和（有序数组） =====
vector<int> twoSum(const vector<int>& nums, int target) {
    int left = 0, right = nums.size() - 1;
    while (left < right) {
        int sum = nums[left] + nums[right];
        if (sum == target) return {left, right};
        else if (sum < target) left++;
        else right--;
    }
    return {};
}

// ===== 三数之和 =====
vector<vector<int>> threeSum(vector<int>& nums) {
    sort(nums.begin(), nums.end());
    vector<vector<int>> res;
    int n = nums.size();
    for (int i = 0; i < n - 2; i++) {
        if (i > 0 && nums[i] == nums[i - 1]) continue;  // 去重
        int left = i + 1, right = n - 1;
        while (left < right) {
            int sum = nums[i] + nums[left] + nums[right];
            if (sum == 0) {
                res.push_back({nums[i], nums[left], nums[right]});
                while (left < right && nums[left] == nums[left + 1]) left++;
                while (left < right && nums[right] == nums[right - 1]) right--;
                left++; right--;
            } else if (sum < 0) left++;
            else right--;
        }
    }
    return res;
}
```

```csharp
// 快慢指针：in-place 删除重复
int RemoveDuplicates(int[] nums) {
    if (nums.Length == 0) return 0;
    int slow = 0;
    for (int fast = 1; fast < nums.Length; fast++)
        if (nums[fast] != nums[slow])
            nums[++slow] = nums[fast];
    return slow + 1;
}

// 左右指针：两数之和
int[] TwoSumSorted(int[] nums, int target) {
    int left = 0, right = nums.Length - 1;
    while (left < right) {
        int sum = nums[left] + nums[right];
        if (sum == target) return new[] { left, right };
        if (sum < target) left++; else right--;
    }
    return Array.Empty<int>();
}
```

### 5.14 链表操作

> **适用场景：** 链表判环、合并有序链表、反转链表、找中点/倒数第 k 个。

```cpp
struct ListNode {
    int val;
    ListNode *next;
    ListNode(int x) : val(x), next(nullptr) {}
};

// ===== 快慢指针判环（Floyd 判圈法） =====
bool hasCycle(ListNode *head) {
    ListNode *slow = head, *fast = head;
    while (fast && fast->next) {
        slow = slow->next;
        fast = fast->next->next;
        if (slow == fast) return true;
    }
    return false;
}

// ===== 合并两个有序链表 =====
ListNode* mergeTwoLists(ListNode *l1, ListNode *l2) {
    ListNode dummy(0), *cur = &dummy;
    while (l1 && l2) {
        if (l1->val <= l2->val) { cur->next = l1; l1 = l1->next; }
        else                    { cur->next = l2; l2 = l2->next; }
        cur = cur->next;
    }
    cur->next = l1 ? l1 : l2;
    return dummy.next;
}

// ===== 反转链表（迭代） =====
ListNode* reverseList(ListNode *head) {
    ListNode *prev = nullptr, *cur = head;
    while (cur) {
        ListNode *next = cur->next;
        cur->next = prev;
        prev = cur;
        cur = next;
    }
    return prev;
}

// ===== 反转链表（递归） =====
ListNode* reverseListRec(ListNode *head) {
    if (!head || !head->next) return head;
    ListNode *last = reverseListRec(head->next);
    head->next->next = head;
    head->next = nullptr;
    return last;
}

// ===== 找链表中间节点 =====
ListNode* middleNode(ListNode *head) {
    ListNode *slow = head, *fast = head;
    while (fast && fast->next) {
        slow = slow->next;
        fast = fast->next->next;
    }
    return slow;
}
```

```csharp
public class ListNode {
    public int val;
    public ListNode next;
    public ListNode(int x) { val = x; }
}

// 快慢指针判环
bool HasCycle(ListNode head) {
    var slow = head; var fast = head;
    while (fast?.next != null) {
        slow = slow.next;
        fast = fast.next.next;
        if (slow == fast) return true;
    }
    return false;
}

// 合并两个有序链表
ListNode MergeTwoLists(ListNode l1, ListNode l2) {
    var dummy = new ListNode(0); var cur = dummy;
    while (l1 != null && l2 != null) {
        if (l1.val <= l2.val) { cur.next = l1; l1 = l1.next; }
        else                  { cur.next = l2; l2 = l2.next; }
        cur = cur.next;
    }
    cur.next = l1 ?? l2;
    return dummy.next;
}

// 反转链表
ListNode ReverseList(ListNode head) {
    ListNode prev = null, cur = head;
    while (cur != null) {
        var next = cur.next;
        cur.next = prev;
        prev = cur;
        cur = next;
    }
    return prev;
}
```

### 5.15 二叉树遍历

> **适用场景：** 二叉树的递归/迭代遍历、层序遍历、最大深度、直径。

```cpp
struct TreeNode {
    int val;
    TreeNode *left, *right;
    TreeNode(int x) : val(x), left(nullptr), right(nullptr) {}
};

// ===== 递归遍历 =====
void preorder(TreeNode *root) {
    if (!root) return;
    // 前序位置
    preorder(root->left);
    // 中序位置
    preorder(root->right);
    // 后序位置
}

// ===== 迭代前序遍历 =====
vector<int> preorderIterative(TreeNode *root) {
    if (!root) return {};
    vector<int> res;
    stack<TreeNode*> st;
    st.push(root);
    while (!st.empty()) {
        TreeNode *node = st.top(); st.pop();
        res.push_back(node->val);
        if (node->right) st.push(node->right);  // 右先入栈（左先出）
        if (node->left)  st.push(node->left);
    }
    return res;
}

// ===== 层序遍历（BFS） =====
vector<vector<int>> levelOrder(TreeNode *root) {
    if (!root) return {};
    vector<vector<int>> res;
    queue<TreeNode*> q;
    q.push(root);
    while (!q.empty()) {
        int sz = q.size();
        vector<int> level;
        for (int i = 0; i < sz; i++) {
            TreeNode *node = q.front(); q.pop();
            level.push_back(node->val);
            if (node->left)  q.push(node->left);
            if (node->right) q.push(node->right);
        }
        res.push_back(level);
    }
    return res;
}

// ===== 二叉树最大深度 =====
int maxDepth(TreeNode *root) {
    if (!root) return 0;
    return 1 + max(maxDepth(root->left), maxDepth(root->right));
}
```

```csharp
public class TreeNode {
    public int val;
    public TreeNode left, right;
    public TreeNode(int x) { val = x; }
}

// 层序遍历（BFS）
IList<IList<int>> LevelOrder(TreeNode root) {
    var res = new List<IList<int>>();
    if (root == null) return res;
    var q = new Queue<TreeNode>();
    q.Enqueue(root);
    while (q.Count > 0) {
        int sz = q.Count;
        var level = new List<int>();
        for (int i = 0; i < sz; i++) {
            var node = q.Dequeue();
            level.Add(node.val);
            if (node.left  != null) q.Enqueue(node.left);
            if (node.right != null) q.Enqueue(node.right);
        }
        res.Add(level);
    }
    return res;
}

// 二叉树最大深度
int MaxDepth(TreeNode root) {
    if (root == null) return 0;
    return 1 + Math.Max(MaxDepth(root.left), MaxDepth(root.right));
}
```

### 5.16 BFS 广度优先搜索

> **适用场景：** 无权图中的最短路径、字符串状态转换、层序遍历。

```cpp
// ===== BFS 最短路径框架 =====
// 从 start 出发，求到 target 的最短距离
int bfs(Node* start, Node* target) {
    queue<Node*> q;
    unordered_set<Node*> visited;
    q.push(start);
    visited.insert(start);
    int step = 0;

    while (!q.empty()) {
        int sz = q.size();
        // 一层一层处理
        for (int i = 0; i < sz; i++) {
            Node* cur = q.front(); q.pop();
            if (cur == target) return step;  // 到达终点
            for (Node* next : cur->neighbors) {
                if (visited.count(next)) continue;
                q.push(next);
                visited.insert(next);
            }
        }
        step++;
    }
    return -1;  // 无法到达
}
```

```csharp
// C# BFS 最短路径框架
int Bfs(Node start, Node target) {
    var q = new Queue<Node>();
    var visited = new HashSet<Node>();
    q.Enqueue(start);
    visited.Add(start);
    int step = 0;

    while (q.Count > 0) {
        int sz = q.Count;
        for (int i = 0; i < sz; i++) {
            var cur = q.Dequeue();
            if (cur == target) return step;
            foreach (var next in cur.Neighbors) {
                if (visited.Contains(next)) continue;
                q.Enqueue(next);
                visited.Add(next);
            }
        }
        step++;
    }
    return -1;
}
```

### 5.17 回溯算法

> **适用场景：** 全排列、子集、组合、N皇后等需要枚举所有可能解的问题。核心框架：做选择 → 递归 → 撤销选择。

```cpp
// ===== 回溯算法框架 =====
// vector<vector<T>> result;   // 存所有解
// vector<T> track;             // 当前路径
void backtrack(/* 参数 */) {
    if (/* 满足结束条件 */) {
        result.push_back(track);
        return;
    }
    for (/* 选择 in 选择列表 */) {
        // 做选择
        track.push_back(choice);
        backtrack(/* 新参数 */);
        // 撤销选择
        track.pop_back();
    }
}

// ===== 全排列（排列问题的标准模板） =====
vector<vector<int>> permute(vector<int>& nums) {
    vector<vector<int>> res;
    vector<int> track;
    vector<bool> used(nums.size(), false);

    function<void()> backtrack = [&]() {
        if (track.size() == nums.size()) {
            res.push_back(track);
            return;
        }
        for (int i = 0; i < nums.size(); i++) {
            if (used[i]) continue;
            used[i] = true;
            track.push_back(nums[i]);
            backtrack();
            track.pop_back();
            used[i] = false;
        }
    };
    backtrack();
    return res;
}

// ===== 子集（组合问题的标准模板） =====
vector<vector<int>> subsets(vector<int>& nums) {
    vector<vector<int>> res;
    vector<int> track;
    function<void(int)> backtrack = [&](int start) {
        res.push_back(track);  // 每个节点都是合法子集
        for (int i = start; i < nums.size(); i++) {
            track.push_back(nums[i]);
            backtrack(i + 1);  // 从 i+1 开始，保证不重复
            track.pop_back();
        }
    };
    backtrack(0);
    return res;
}
```

```csharp
// C# 子集
IList<IList<int>> Subsets(int[] nums) {
    var res = new List<IList<int>>();
    var track = new List<int>();

    void Backtrack(int start) {
        res.Add(new List<int>(track));
        for (int i = start; i < nums.Length; i++) {
            track.Add(nums[i]);
            Backtrack(i + 1);
            track.RemoveAt(track.Count - 1);
        }
    }
    Backtrack(0);
    return res;
}

// C# 全排列
IList<IList<int>> Permute(int[] nums) {
    var res = new List<IList<int>>();
    var track = new List<int>();
    var used = new bool[nums.Length];

    void Backtrack() {
        if (track.Count == nums.Length) {
            res.Add(new List<int>(track));
            return;
        }
        for (int i = 0; i < nums.Length; i++) {
            if (used[i]) continue;
            used[i] = true; track.Add(nums[i]);
            Backtrack();
            used[i] = false; track.RemoveAt(track.Count - 1);
        }
    }
    Backtrack();
    return res;
}
```

### 5.18 拓扑排序（Kahn 算法）

> **适用场景：** 有向无环图（DAG）的依赖排序、课程安排、任务调度。

```cpp
// Kahn 算法（BFS）：计算入度，入度为 0 的节点先处理
vector<int> topologicalSort(int n, const vector<vector<int>>& edges) {
    vector<vector<int>> g(n);
    vector<int> inDegree(n, 0);

    for (auto& e : edges) {
        // e[0] 依赖 e[1]，即 e[1] → e[0]
        g[e[1]].push_back(e[0]);
        inDegree[e[0]]++;
    }

    queue<int> q;
    for (int i = 0; i < n; i++)
        if (inDegree[i] == 0) q.push(i);

    vector<int> order;
    while (!q.empty()) {
        int node = q.front(); q.pop();
        order.push_back(node);
        for (int next : g[node])
            if (--inDegree[next] == 0) q.push(next);
    }

    if (order.size() != n) return {};  // 有环，无法拓扑排序
    return order;
}
```

```csharp
// C# 拓扑排序
int[] TopologicalSort(int n, int[][] edges) {
    var g = Enumerable.Range(0, n).Select(_ => new List<int>()).ToArray();
    var inDegree = new int[n];

    foreach (var e in edges) {
        // e[0] 依赖 e[1]，即 e[1] → e[0]
        g[e[1]].Add(e[0]);
        inDegree[e[0]]++;
    }

    var q = new Queue<int>();
    for (int i = 0; i < n; i++)
        if (inDegree[i] == 0) q.Enqueue(i);

    var order = new List<int>();
    while (q.Count > 0) {
        int node = q.Dequeue();
        order.Add(node);
        foreach (int next in g[node])
            if (--inDegree[next] == 0) q.Enqueue(next);
    }
    return order.Count == n ? order.ToArray() : Array.Empty<int>();
}
```

### 5.19 Dijkstra 最短路径

> **适用场景：** 有权图（非负权边）的单源最短路径。时间复杂度 O((V+E) log V)。

```cpp
using pii = pair<int, int>;

// Dijkstra：求从源点 src 到所有节点的最短距离
vector<int> dijkstra(const vector<vector<pii>>& graph, int src) {
    // graph[u] = {(v, w), ...} 表示 u → v 权值为 w 的边
    int n = graph.size();
    vector<int> dist(n, INT_MAX);
    dist[src] = 0;

    // 小顶堆：{距离, 节点}
    priority_queue<pii, vector<pii>, greater<pii>> pq;
    pq.push({0, src});

    while (!pq.empty()) {
        auto [d, u] = pq.top(); pq.pop();
        if (d > dist[u]) continue;  // 过期状态，跳过

        for (auto [v, w] : graph[u]) {
            int nd = d + w;
            if (nd < dist[v]) {
                dist[v] = nd;
                pq.push({nd, v});
            }
        }
    }
    return dist;
}

// 建图示例
// int n, m; cin >> n >> m;
// vector<vector<pii>> graph(n);
// for (int i = 0; i < m; i++) {
//     int u, v, w; cin >> u >> v >> w;
//     graph[u].push_back({v, w});  // 有向图
// }
```

```csharp
// C# Dijkstra
int[] Dijkstra(List<(int to, int w)>[] graph, int src) {
    int n = graph.Length;
    var dist = new int[n];
    Array.Fill(dist, int.MaxValue);
    dist[src] = 0;

    // 小顶堆：(距离, 节点)
    var pq = new PriorityQueue<int, int>();
    pq.Enqueue(src, 0);

    while (pq.Count > 0) {
        pq.TryDequeue(out int u, out int d);
        if (d > dist[u]) continue;
        foreach (var (v, w) in graph[u]) {
            int nd = d + w;
            if (nd < dist[v]) {
                dist[v] = nd;
                pq.Enqueue(v, nd);
            }
        }
    }
    return dist;
}
```

### 5.20 单调栈

> **适用场景：** 下一个更大/更小元素、每日温度、柱状图最大矩形。O(N)，每个元素入栈/出栈各一次。

```cpp
// ===== 下一个更大元素（从前往后遍历） =====
vector<int> nextGreaterElement(const vector<int>& nums) {
    int n = nums.size();
    vector<int> res(n, -1);
    stack<int> st;  // 存下标，栈内值单调递减

    for (int i = 0; i < n; i++) {
        while (!st.empty() && nums[st.top()] < nums[i]) {
            res[st.top()] = nums[i];
            st.pop();
        }
        st.push(i);
    }
    return res;
}

// ===== 循环数组：下一个更大元素 II（虚拟翻倍） =====
vector<int> nextGreaterElements(const vector<int>& nums) {
    int n = nums.size();
    vector<int> res(n, -1);
    stack<int> st;

    for (int i = 0; i < 2 * n; i++) {
        while (!st.empty() && nums[st.top()] < nums[i % n]) {
            res[st.top()] = nums[i % n];
            st.pop();
        }
        if (i < n) st.push(i);
    }
    return res;
}
```

```csharp
// C# 下一个更大元素
int[] NextGreaterElement(int[] nums) {
    int n = nums.Length;
    var res = new int[n];
    Array.Fill(res, -1);
    var st = new Stack<int>();

    for (int i = 0; i < n; i++) {
        while (st.Count > 0 && nums[st.Peek()] < nums[i]) {
            res[st.Pop()] = nums[i];
        }
        st.Push(i);
    }
    return res;
}
```

### 5.21 单调队列（滑动窗口最大值）

> **适用场景：** 固定长度滑动窗口中的最值问题。O(N)，每个元素入队/出队各一次。

```cpp
// ===== 滑动窗口最大值 =====
vector<int> maxSlidingWindow(const vector<int>& nums, int k) {
    int n = nums.size();
    vector<int> res;
    deque<int> dq;  // 存下标，队头始终是窗口最大值的下标

    for (int i = 0; i < n; i++) {
        // 1. 移除不在窗口内的队头
        while (!dq.empty() && dq.front() < i - k + 1)
            dq.pop_front();

        // 2. 维护单调递减：移除所有比 nums[i] 小的队尾
        while (!dq.empty() && nums[dq.back()] < nums[i])
            dq.pop_back();

        dq.push_back(i);

        // 3. 窗口形成后记录答案
        if (i >= k - 1)
            res.push_back(nums[dq.front()]);
    }
    return res;
}
```

```csharp
// C# 滑动窗口最大值
int[] MaxSlidingWindow(int[] nums, int k) {
    int n = nums.Length;
    var res = new int[n - k + 1];
    var dq = new LinkedList<int>();  // 存下标

    for (int i = 0; i < n; i++) {
        // 移除过期队头
        while (dq.Count > 0 && dq.First.Value < i - k + 1)
            dq.RemoveFirst();
        // 维护单调递减
        while (dq.Count > 0 && nums[dq.Last.Value] < nums[i])
            dq.RemoveLast();
        dq.AddLast(i);
        // 记录结果
        if (i >= k - 1)
            res[i - k + 1] = nums[dq.First.Value];
    }
    return res;
}
```

### 5.22 字典树 Trie

> **适用场景：** 高效存储和检索字符串集合，特别适合前缀匹配（自动补全、拼写检查）。

```cpp
// ===== Trie（前缀树） =====
struct TrieNode {
    TrieNode* children[26];
    bool isEnd;
    TrieNode() : isEnd(false) {
        for (int i = 0; i < 26; i++) children[i] = nullptr;
    }
};

class Trie {
    TrieNode* root;
public:
    Trie() { root = new TrieNode(); }

    void insert(string word) {
        TrieNode* node = root;
        for (char c : word) {
            int idx = c - 'a';
            if (!node->children[idx])
                node->children[idx] = new TrieNode();
            node = node->children[idx];
        }
        node->isEnd = true;
    }

    bool search(string word) {
        TrieNode* node = root;
        for (char c : word) {
            int idx = c - 'a';
            if (!node->children[idx]) return false;
            node = node->children[idx];
        }
        return node->isEnd;
    }

    bool startsWith(string prefix) {
        TrieNode* node = root;
        for (char c : prefix) {
            int idx = c - 'a';
            if (!node->children[idx]) return false;
            node = node->children[idx];
        }
        return true;
    }
};
```

```csharp
// C# Trie
public class Trie {
    private Trie[] children = new Trie[26];
    private bool isEnd = false;

    public void Insert(string word) {
        Trie node = this;
        foreach (char c in word) {
            int idx = c - 'a';
            node.children[idx] ??= new Trie();
            node = node.children[idx];
        }
        node.isEnd = true;
    }

    public bool Search(string word) {
        Trie node = this;
        foreach (char c in word) {
            int idx = c - 'a';
            if (node.children[idx] == null) return false;
            node = node.children[idx];
        }
        return node.isEnd;
    }

    public bool StartsWith(string prefix) {
        Trie node = this;
        foreach (char c in prefix) {
            int idx = c - 'a';
            if (node.children[idx] == null) return false;
            node = node.children[idx];
        }
        return true;
    }
}
```

### 5.23 动态规划框架

> **适用场景：** 最优子结构 + 重叠子问题的优化求解。两种范式：自顶向下（递归+备忘录）和自底向上（迭代 DP table）。

```cpp
// ===== 自顶向下（递归 + 备忘录） =====
// 以斐波那契数列为例
vector<int> memo;

int fib(int n) {
    if (n == 0 || n == 1) return n;
    if (memo[n] != -1) return memo[n];
    memo[n] = fib(n - 1) + fib(n - 2);
    return memo[n];
}

// 使用：memo.assign(n + 1, -1); cout << fib(n);

// ===== 自底向上（迭代 DP Table） =====
// 通用框架：
// dp[0..n] 初始化 base case
// for 状态1 in 所有可能值:
//     for 状态2 in 所有可能值:
//         dp[状态1][状态2] = 求最优(选择1, 选择2, ...)

// 示例：零钱兑换（最少硬币数凑成金额）
int coinChange(const vector<int>& coins, int amount) {
    vector<int> dp(amount + 1, amount + 1);  // 初始化为最大
    dp[0] = 0;  // base case
    for (int i = 1; i <= amount; i++) {
        for (int coin : coins) {
            if (i >= coin)
                dp[i] = min(dp[i], dp[i - coin] + 1);
        }
    }
    return dp[amount] > amount ? -1 : dp[amount];
}

// 示例：最长递增子序列 LIS
int lengthOfLIS(const vector<int>& nums) {
    int n = nums.size();
    vector<int> dp(n, 1);  // base case：每个元素自身长度为 1
    for (int i = 1; i < n; i++)
        for (int j = 0; j < i; j++)
            if (nums[j] < nums[i])
                dp[i] = max(dp[i], dp[j] + 1);
    return *max_element(dp.begin(), dp.end());
}

// 示例：0-1 背包
int knapsack(int W, const vector<int>& wt, const vector<int>& val) {
    int n = wt.size();
    vector<vector<int>> dp(n + 1, vector<int>(W + 1, 0));
    for (int i = 1; i <= n; i++) {
        for (int w = 1; w <= W; w++) {
            if (w < wt[i - 1])
                dp[i][w] = dp[i - 1][w];  // 装不下
            else
                dp[i][w] = max(dp[i - 1][w], dp[i - 1][w - wt[i - 1]] + val[i - 1]);
        }
    }
    return dp[n][W];
}
```

```csharp
// C# 零钱兑换
int CoinChange(int[] coins, int amount) {
    int[] dp = new int[amount + 1];
    Array.Fill(dp, amount + 1);
    dp[0] = 0;
    for (int i = 1; i <= amount; i++)
        foreach (int coin in coins)
            if (i >= coin)
                dp[i] = Math.Min(dp[i], dp[i - coin] + 1);
    return dp[amount] > amount ? -1 : dp[amount];
}

// C# LIS
int LengthOfLIS(int[] nums) {
    int n = nums.Length;
    int[] dp = new int[n];
    Array.Fill(dp, 1);
    for (int i = 1; i < n; i++)
        for (int j = 0; j < i; j++)
            if (nums[j] < nums[i])
                dp[i] = Math.Max(dp[i], dp[j] + 1);
    return dp.Max();
}
```

---

## 六、常见输入格式速查表

| 输入格式 | 示例 | C# | C++ |
|---------|------|----|-----|
| 一个整数 | `5` | `int n = int.Parse(Console.ReadLine());` | `int n; cin >> n;` |
| 两个整数 | `3 5` | `var p = C.ReadLine().Split(); int a=int.Parse(p[0]), b=int.Parse(p[1]);` | `int a,b; cin >> a >> b;` |
| 第一行 T，后面 T 行 | `2\n1 2\n3 4` | 见 3.4 | 见 4.4 |
| 先给 n，再给一行 n 个数 | `5\n1 2 3 4 5` | `int n=int.Parse(CLR); var arr=CLR.Split()...` | `cin>>n; vi a(n); for(...) cin>>a[i];` |
| 一行数组（未知长度） | `1 2 3 4` | `var arr=CLR.Split().Select(int.Parse).ToArray();` | 见 4.2 |
| 多组直到 EOF | 不定行数 | `while((line=CLR)!=null)` | `while(cin>>a>>b)` |
| 先给 n，再给 n 行字符串 | `2\nhello\nworld` | 见 3.5 | 见 4.5（注意 ignore） |
| 矩阵 | 行/列 + 数据 | 见 3.6 | 见 4.6 |
| n m + 后面 m 条边（图） | `3 3\n1 2\n2 3\n1 3` | 见 5.2 | 见 5.2 |

> **`CLR`** = `Console.ReadLine()`（C#）

---

## 七、最容易踩的坑

### C# 坑

```csharp
// 1. Split 遇到空字符串
string[] parts = Console.ReadLine().Split();
if (parts.Length == 0) continue;   // ← 处理空行

// 安全做法：
string line = Console.ReadLine();
if (string.IsNullOrWhiteSpace(line)) continue;
int[] arr = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse).ToArray();

// 2. 大输入用 LINQ 很慢
// ❌ 10^5 级别：Console.ReadLine().Split().Select(int.Parse).ToArray()
// ✅ 用 FastReader（见 2.2）或手动 for 循环解析

// 3. StringBuilder 比多次 Console.WriteLine 快很多
```

### C++ 坑

```cpp
// 1. cin 和 getline 混用忘记 ignore
int n;
cin >> n;
// 缓冲区: "42\n"
string s;
getline(cin, s);  // 读到的是 ""（空串）！
// ✅ 正确：
cin >> n;
cin.ignore();     // 吃掉 \n
getline(cin, s);

// 2. endl 会 flush，大量输出巨慢
// ❌ cout << x << endl;
// ✅ cout << x << "\n";

// 3. vector<bool> 不是 bool 数组，是 bitset！
// 用 vector<char> 或 deque<bool> 代替

// 4. 开数组注意栈溢出
// ❌ int arr[1000000];   // 本地可能爆栈
// ✅ vector<int> arr(1000000);  // 堆分配
```
