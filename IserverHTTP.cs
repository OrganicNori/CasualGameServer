using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Web; // 需要添加这个命名空间

namespace MyServer // 修改命名空间
{
    class ClientState
    {
        public Socket socket;
        public byte[] readBuff = new byte[1024];
    }

    class MainClass
    {
        static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

        public static void Main(string[] args)
        {
            Console.WriteLine("启动服务器...");

            // 启动数据库连接
            bool dbConnected = DBManager.Connect("localhost:6379");
            if (dbConnected)
            {
                Console.WriteLine("链接数据库成功");
                DBManager.InitializeSampleItems();
            }

            // Socket设置
            Socket listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 8888);
            listenfd.Bind(ipEp);
            listenfd.Listen(0);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[服务器] 启动成功，监听端口 8888");
            Console.WriteLine("********************");
            Console.WriteLine($"[服务器]请访问: http://{ipAdr}:8888");
            Console.ResetColor();

            // 异步服务器
            listenfd.BeginAccept(AcceptCallback, listenfd);

            Console.ReadLine();

            // 程序退出时断开数据库连接
            DBManager.Disconnect();
        }

        // Accept回调
        public static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[服务器] 接受新连接");
                Console.ResetColor();

                Socket listenfd = (Socket)ar.AsyncState;
                Socket clientfd = listenfd.EndAccept(ar);

                ClientState state = new ClientState();
                state.socket = clientfd;
                clients.Add(clientfd, state);

                clientfd.BeginReceive(state.readBuff, 0, 1024, 0, ReceiveCallback, state);
                listenfd.BeginAccept(AcceptCallback, listenfd);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Socket Accept失败: " + ex.ToString());
            }
        }

        // Receive回调
        public static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                ClientState state = (ClientState)ar.AsyncState;
                Socket clientfd = state.socket;
                int count = clientfd.EndReceive(ar);

                if (count == 0)
                {
                    clientfd.Close();
                    clients.Remove(clientfd);
                    Console.WriteLine("Socket关闭");
                    return;
                }

                string recvStr = Encoding.UTF8.GetString(state.readBuff, 0, count);
                Console.WriteLine("收到HTTP请求:\n" + recvStr);

                string[] lines = recvStr.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length > 0)
                {
                    string requestLine = lines[0];
                    string[] parts = requestLine.Split(' ');

                    if (parts.Length >= 2)
                    {
                        string method = parts[0];
                        string url = parts[1];
                        string responseBody = "";
                        string contentType = "text/html; charset=utf-8";

                        // GET请求处理
                        if (method == "GET")
                        {
                            responseBody = HandleGetRequest(url);
                        }
                        // POST请求处理
                        else if (method == "POST")
                        {
                            responseBody = HandlePostRequest(url, recvStr);
                        }

                        // 发送HTTP响应
                        SendHttpResponse(clientfd, responseBody, contentType);
                        Console.WriteLine("已响应 " + method + " " + url);
                    }
                }

                clientfd.Close();
                clients.Remove(clientfd);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Socket Receive失败: " + ex.ToString());
            }
        }

        // 处理GET请求
        private static string HandleGetRequest(string url)
        {
            switch (url)
            {
                case "/":
                case "/index":
                    return HandleHomePage();
                case "/login":
                    return HandleLogin();
                case "/inventory":
                    return HandleInventory();
                case "/add-item":
                    return HandleAddItemPage();
                case "/db-status":
                    return HandleDbStatus();
                case "/clear-data":
                    return HandleClearDataPage();
                case "/notice":  // 添加公告页面的GET路由
                    return HandleGetNotice();

                default:
                    return HandleNotFound(url);
            }
        }

        // 处理POST请求
        private static string HandlePostRequest(string url, string requestData)
        {
            switch (url)
            {
                case "/login":
                    return HandleLoginPost(requestData);
                case "/add-item":
                    return HandleAddItemPost(requestData);
                case "/delete-item":
                    return HandleDeleteItemPost(requestData);
                case "/update-item":
                    return HandleUpdateItemPost(requestData);
                case "/clear-data":
                    return HandleClearDataPost();
                default:
                    return HandleNotFound(url);
            }
        }


        // 发送HTTP响应
        private static void SendHttpResponse(Socket clientfd, string responseBody, string contentType)
        {
            string responseHeader = "HTTP/1.1 200 OK\r\n" +
                                   "Content-Type: " + contentType + "\r\n" +
                                   "Content-Length: " + Encoding.UTF8.GetByteCount(responseBody) + "\r\n" +
                                   "Connection: close\r\n" +
                                   "\r\n";

            string httpResponse = responseHeader + responseBody;
            byte[] sendBytes = Encoding.UTF8.GetBytes(httpResponse);
            clientfd.Send(sendBytes);
        }

        // 首页
        private static string HandleHomePage()
        {
            return "<html><head><title>游戏服务器</title></head><body>" +
                   "<h1>欢迎来到超级精简游戏服务器</h1>" +
                   "<div style='border: 1px solid #ccc; padding: 10px; margin: 10px;'>" +
                   "<h2>数据库状态: " + (DBManager.IsConnected ? "<span style='color:green'>已连接</span>" : "<span style='color:red'>未连接</span>") + "</h2>" +
                   "</div>" +
                   "<ul>" +
                   "<li><a href='/inventory'>查看背包</a></li>" +
                   "<li><a href='/add-item'>添加道具</a></li>" +
                   "<li><a href='/db-status'>数据库操作</a></li>" +
                   "<li><a href='/notice'>查看公告</a></li>" +
                   "<li><a href='/login'>登录</a></li>" +
                   "<li><a href='/clear-data' style='color:red; font-weight:bold;'>清空所有数据</a></li>" +
           "</ul>" +
           "<div style='margin-top: 20px; padding: 10px; background-color: #fff3cd; border: 1px solid #ffeaa7;'>" +
           "<p><strong>注意：</strong>清空数据操作不可恢复，请谨慎使用！</p>" +
                     "</div>" +
           "</body></html>";
        }

        // 公告处理
        private static string HandleGetNotice()
        {
            // 这里可以从数据库或Redis获取公告
            return "<html><body>" +
                   "<h1>最新公告</h1>" +
                   "<ul>" +
                   "<li>公告1: 服务器维护通知</li>" +
                   "<li>公告2: 新功能上线</li>" +
                   "<li>公告3: 用户须知</li>" +
                   "</ul>" +
                   "<a href='/'>返回首页</a>" +
                   "</body></html>";
        }

        // 登录页面
        private static string HandleLogin()
        {
            return "<html><body>" +
                   "<h1>用户登录</h1>" +
                   "<form method='post'>" +
                   "用户名: <input type='text' name='username'><br>" +
                   "密码: <input type='password' name='password'><br>" +
                   "<input type='submit' value='登录'>" +
                   "</form>" +
                   "<a href='/'>返回首页</a>" +
                   "</body></html>";
        }

        // 查看背包
        private static string HandleInventory()
        {
            var items = DBManager.GetAllItems();
            StringBuilder sb = new StringBuilder();

            sb.Append("<html><head><title>背包道具</title></head><body>");
            sb.Append("<h1>背包道具</h1>");
            sb.Append("<a href='/'>返回首页</a> | <a href='/add-item'>添加道具</a>");
            sb.Append("<table border='1' style='border-collapse: collapse; margin: 10px;'>");
            sb.Append("<tr><th>ID</th><th>名称</th><th>数量</th><th>描述</th><th>创建时间</th><th>操作</th></tr>");

            foreach (var item in items)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{item["id"]}</td>");
                sb.Append($"<td>{item["name"]}</td>");
                sb.Append($"<td>{item["quantity"]}</td>");
                sb.Append($"<td>{item["description"]}</td>");
                sb.Append($"<td>{item["createTime"]}</td>");
                sb.Append($"<td>" +
                         $"<form method='post' action='/delete-item' style='display:inline;'>" +
                         $"<input type='hidden' name='itemId' value='{item["id"]}'>" +
                         $"<input type='submit' value='删除' onclick='return confirm(\"确定删除吗？\")'>" +
                         $"</form>" +
                         $"</td>");
                sb.Append("</tr>");
            }

            sb.Append("</table>");
            sb.Append("</body></html>");

            return sb.ToString();
        }

        // 添加道具页面
        private static string HandleAddItemPage()
        {
            return "<html><body>" +
                   "<h1>添加新道具</h1>" +
                   "<form method='post'>" +
                   "道具ID: <input type='text' name='itemId'><br>" +
                   "道具名称: <input type='text' name='itemName'><br>" +
                   "数量: <input type='number' name='quantity' value='1'><br>" +
                   "描述: <textarea name='description'></textarea><br>" +
                   "<input type='submit' value='添加道具'>" +
                   "</form>" +
                   "<a href='/inventory'>返回背包</a>" +
                   "</body></html>";
        }

        // 数据库状态页面
        private static string HandleDbStatus()
        {
            return "<html><body>" +
                   "<h1>数据库操作</h1>" +
                   "<div style='margin: 10px;'>" +
                   "<form method='post' action='/add-item' style='display:inline; margin-right: 10px;'>" +
                   "<input type='hidden' name='itemId' value='auto_generated'>" +
                   "<input type='hidden' name='itemName' value='测试道具'>" +
                   "<input type='hidden' name='quantity' value='1'>" +
                   "<input type='hidden' name='description' value='自动生成的测试道具'>" +
                   "<input type='submit' value='添加测试道具'>" +
                   "</form>" +

                   "<form method='post' action='/update-item' style='display:inline; margin-right: 10px;'>" +
                   "道具ID: <input type='text' name='itemId' value='item001'>" +
                   "新数量: <input type='number' name='quantity' value='10'>" +
                   "<input type='submit' value='更新数量'>" +
                   "</form>" +
                   "</div>" +
                   "<a href='/'>返回首页</a>" +
                   "</body></html>";
        }

        // 404页面
        private static string HandleNotFound(string url)
        {
            return "<html><body>" +
                   "<h1>404 - 页面未找到</h1>" +
                   "<p>请求的URL: " + url + " 不存在</p>" +
                   "<a href='/'>返回首页</a>" +
                   "</body></html>";
        }

        // 处理登录POST
        private static string HandleLoginPost(string requestData)
        {
            try
            {
                var formData = ParseFormData(requestData);
                string username = formData.ContainsKey("username") ? formData["username"] : "";
                string password = formData.ContainsKey("password") ? formData["password"] : "";

                Console.WriteLine("收到登录数据 - 用户名: " + username + ", 密码: " + password);

                // 简易密码验证
                if (password == "1234567")
                {
                    return "<html><body>" +
                           "<h1 style='color:green;'>登录成功！</h1>" +
                           "<p>欢迎用户: " + username + "</p>" +
                           "<p>密码验证通过！</p>" +
                           "<a href='/'>返回首页</a>" +
                           "</body></html>";
                }
                else
                {
                    return "<html><body>" +
                           "<h1 style='color:red;'>登录失败</h1>" +
                           "<p>密码错误！正确密码是: 1234567</p>" +
                           "<a href='/login'>重新登录</a>" +
                           "</body></html>";
                }
            }
            catch (Exception ex)
            {
                return "<html><body>" +
                       "<h1>服务器错误</h1>" +
                       "<p>处理登录请求时出错: " + ex.Message + "</p>" +
                       "<a href='/login'>返回登录页</a>" +
                       "</body></html>";
            }
        }

        // 添加道具POST处理
        private static string HandleAddItemPost(string requestData)
        {
            try
            {
                var formData = ParseFormData(requestData);
                string itemId = formData.ContainsKey("itemId") ? formData["itemId"] : "";
                string itemName = formData.ContainsKey("itemName") ? formData["itemName"] : "";
                string quantityStr = formData.ContainsKey("quantity") ? formData["quantity"] : "1";
                string description = formData.ContainsKey("description") ? formData["description"] : "";

                if (itemId == "auto_generated" || string.IsNullOrEmpty(itemId))
                {
                    itemId = "item_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                }

                if (string.IsNullOrEmpty(itemName))
                {
                    return "<html><body><h1>错误</h1><p>道具名称不能为空</p><a href='/add-item'>返回</a></body></html>";
                }

                if (!int.TryParse(quantityStr, out int quantity))
                {
                    quantity = 1;
                }

                bool success = DBManager.StoreItem(itemId, itemName, quantity, description);

                if (success)
                {
                    return "<html><body>" +
                           "<h1 style='color:green;'>道具添加成功！</h1>" +
                           "<p>道具ID: " + itemId + "</p>" +
                           "<p>道具名称: " + itemName + "</p>" +
                           "<p>数量: " + quantity + "</p>" +
                           "<a href='/inventory'>查看背包</a> | <a href='/add-item'>继续添加</a>" +
                           "</body></html>";
                }
                else
                {
                    return "<html><body>" +
                           "<h1 style='color:red;'>道具添加失败</h1>" +
                           "<p>请检查数据库连接</p>" +
                           "<a href='/add-item'>返回</a>" +
                           "</body></html>";
                }
            }
            catch (Exception ex)
            {
                return "<html><body>" +
                       "<h1>服务器错误</h1>" +
                       "<p>处理请求时出错: " + ex.Message + "</p>" +
                       "<a href='/add-item'>返回</a>" +
                       "</body></html>";
            }
        }

        // 删除道具POST处理
        private static string HandleDeleteItemPost(string requestData)
        {
            try
            {
                var formData = ParseFormData(requestData);
                string itemId = formData.ContainsKey("itemId") ? formData["itemId"] : "";

                if (string.IsNullOrEmpty(itemId))
                {
                    return "<html><body><h1>错误</h1><p>道具ID不能为空</p><a href='/inventory'>返回背包</a></body></html>";
                }

                bool success = DBManager.DeleteItem(itemId);

                if (success)
                {
                    return "<html><body>" +
                           "<h1 style='color:green;'>道具删除成功！</h1>" +
                           "<p>已删除道具: " + itemId + "</p>" +
                           "<a href='/inventory'>返回背包</a>" +
                           "</body></html>";
                }
                else
                {
                    return "<html><body>" +
                           "<h1 style='color:red;'>道具删除失败</h1>" +
                           "<p>道具可能不存在或数据库连接问题</p>" +
                           "<a href='/inventory'>返回背包</a>" +
                           "</body></html>";
                }
            }
            catch (Exception ex)
            {
                return "<html><body>" +
                       "<h1>服务器错误</h1>" +
                       "<p>处理请求时出错: " + ex.Message + "</p>" +
                       "<a href='/inventory'>返回背包</a>" +
                       "</body></html>";
            }
        }

        // 更新道具数量POST处理
        private static string HandleUpdateItemPost(string requestData)
        {
            try
            {
                var formData = ParseFormData(requestData);
                string itemId = formData.ContainsKey("itemId") ? formData["itemId"] : "";
                string quantityStr = formData.ContainsKey("quantity") ? formData["quantity"] : "";

                if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(quantityStr))
                {
                    return "<html><body><h1>错误</h1><p>道具ID和数量不能为空</p><a href='/db-status'>返回</a></body></html>";
                }

                if (!int.TryParse(quantityStr, out int quantity))
                {
                    return "<html><body><h1>错误</h1><p>数量必须是数字</p><a href='/db-status'>返回</a></body></html>";
                }

                bool success = DBManager.UpdateItemQuantity(itemId, quantity);

                if (success)
                {
                    return "<html><body>" +
                           "<h1 style='color:green;'>道具更新成功！</h1>" +
                           "<p>道具ID: " + itemId + "</p>" +
                           "<p>新数量: " + quantity + "</p>" +
                           "<a href='/inventory'>查看背包</a> | <a href='/db-status'>返回</a>" +
                           "</body></html>";
                }
                else
                {
                    return "<html><body>" +
                           "<h1 style='color:red;'>道具更新失败</h1>" +
                           "<p>道具可能不存在或数据库连接问题</p>" +
                           "<a href='/db-status'>返回</a>" +
                           "</body></html>";
                }
            }
            catch (Exception ex)
            {
                return "<html><body>" +
                       "<h1>服务器错误</h1>" +
                       "<p>处理请求时出错: " + ex.Message + "</p>" +
                       "<a href='/db-status'>返回</a>" +
                       "</body></html>";
            }
        }

        // 解析表单数据
        private static Dictionary<string, string> ParseFormData(string requestData)
        {
            var formData = new Dictionary<string, string>();
            string[] lines = requestData.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            bool foundEmptyLine = false;
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    foundEmptyLine = true;
                    continue;
                }
                if (foundEmptyLine)
                {
                    string[] pairs = line.Split('&');
                    foreach (string pair in pairs)
                    {
                        string[] keyValue = pair.Split('=');
                        if (keyValue.Length == 2)
                        {
                            string key = keyValue[0];
                            string value = HttpUtility.UrlDecode(keyValue[1]); // 使用 System.Web
                            formData[key] = value;
                        }
                    }
                    break;
                }
            }

            return formData;
        }


        // 添加清空数据页面
        private static string HandleClearDataPage()
        {
            return "<html><body>" +
                   "<h1 style='color:red;'>危险操作</h1>" +
                   "<p>这将删除所有道具数据！</p>" +
                   "<form method='post' onsubmit='return confirm(\"确定要清空所有数据吗？此操作不可撤销！\")'>" +
                   "<input type='submit' value='确认清空所有数据' style='background-color:red;color:white;'>" +
                   "</form>" +
                   "<br>" +
                   "<a href='/'>返回首页</a>" +
                   "</body></html>";
        }

        // 处理清空数据POST
        private static string HandleClearDataPost()
        {
            try
            {
                bool success = DBManager.ClearAllItems();
                if (success)
                {
                    return "<html><body>" +
                           "<h1 style='color:green;'>数据清空成功</h1>" +
                           "<p>所有道具数据已被清空</p>" +
                           "<a href='/'>返回首页</a>" +
                           "</body></html>";
                }
                else
                {
                    return "<html><body>" +
                           "<h1 style='color:red;'>数据清空失败</h1>" +
                           "<a href='/'>返回首页</a>" +
                           "</body></html>";
                }
            }
            catch (Exception ex)
            {
                return "<html><body>" +
                       "<h1>服务器错误</h1>" +
                       "<p>处理请求时出错: " + ex.Message + "</p>" +
                       "<a href='/'>返回首页</a>" +
                       "</body></html>";
            }
        }

    }
}