using System;
using System.IO;
using System.Threading.Tasks;
using Nethereum.Web3;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System.Numerics;
using Nethereum.Web3.Accounts;
using AngleSharp;
using AngleSharp.Dom;

[Function("balanceOf")]
public class BalanceOf : FunctionMessage
{
    [Parameter("address", "account", 1)]
    public string acc { get; set; }
}

[Function("bid")]
public class Bid : FunctionMessage
{
    [Parameter("uint256", "_tokenId", 1)]
    public BigInteger bidTokenId { get; set; }

    [Parameter("uint256", "_amount", 2)]
    public BigInteger bidPrice { get; set; }
}

[Function("createSaleAuction")]
public class CreateSaleAuction : FunctionMessage
{
    [Parameter("uint256", "_tokenId", 1)]
    public BigInteger saleTokenId { get; set; }

    [Parameter("uint256", "_startingPrice", 2)]
    public BigInteger salePrice1 { get; set; }

    [Parameter("uint256", "_endingPrice", 3)]
    public BigInteger salePrice2 { get; set; }

    [Parameter("uint256", "_duration", 4)]
    public BigInteger saleDuration { get; set; }
}


class Program
{
    static double[,] plant_price = new double[9, 4];
    static long plantId = 0;
    static JObject JO = new JObject();
    static string token_id64 = "";
    static string tokenId = "";
    static BigInteger tokenIdHex;
    static HexBigInteger tokenIdTemp;
    static BigInteger priceHex;
    static HexBigInteger priceTemp;
    static string price = "";
    static string bidContractAddress = "0x926eae99527a9503eadb4c9e927f21f84f20c977";
    static string saleContractAddress = "0x5ab19e7091dd208f352f8e727b6dcc6f8abb6275";
    static string check_buy = "0";

    static double balance_PVU = 0;
    static string balance = "";
    static double down_limit = 0.82;
    static int count = 12;
    static string[] url_web3 = new string[3] { "https://bsc-dataseed.binance.org/", "https://bsc-dataseed1.defibit.io/", "https://bsc-dataseed1.ninicoin.io/" };
    static int ind = 0;
    static string urlWeb3 = "";
    static int saleOn = 0;
    static double koef = 1.07;
    static int iter = 0;
    static double saleC = 1.03;
    static int inputdata_method;

    static bool info_checker = false;

    static HexBigInteger gas = new HexBigInteger(BigInteger.Parse("300000"));
    static HexBigInteger gasPrice = new HexBigInteger(BigInteger.Parse("5000000000"));

    static void Main(string[] args)
    {
        int countFor = 0;
        //________________________
        for (int i = 0; i < 9; i++)
            for (int j = 0; j < 4; j++)
                plant_price[i, j] = 0;
        //------------------------

        Console.Write("Enter down limit: ");
        down_limit = Convert.ToDouble(Console.ReadLine());

        Console.Write("Enter koef: ");
        koef = Convert.ToDouble(Console.ReadLine());

        Console.Write("Enter count: ");
        count = Convert.ToInt32(Console.ReadLine());

        Console.Write("Enter countFor: ");
        countFor = Convert.ToInt32(Console.ReadLine());

        Console.Write("Enter the number of url web3 (0-2): ");
        ind = Convert.ToInt32(Console.ReadLine());
        urlWeb3 = url_web3[ind];

        Console.Write("Choose option(0 - turn off Sale, 1 - turn on Sale): ");
        saleOn = Convert.ToInt32(Console.ReadLine());

        if (saleOn == 1)
        {
            Console.Write("Enter saleC: ");
            saleC = Convert.ToDouble(Console.ReadLine());
        }

        Console.Write("Choose input data method (0 - site, 1 - file): ");
        inputdata_method = Convert.ToInt32(Console.ReadLine());

        if (inputdata_method == 1)
        {
            data_from_file();
        }

        for (int c1 = 0; c1 < 1000000; c1++)
        {
            if (inputdata_method == 0)
            {
                selectData().Wait();
            }

            getBalance().Wait();

            for (int c2 = 0; c2 < countFor; c2++)
            {
                info_checker = false;
                GetInfo().Wait();
                if (tokenId != "" && info_checker && priceHex > 1000000000000000000 && tokenIdHex < 1000000000)
                {
                    //Console.WriteLine("Есть токен!"); <-----
                    GetPlantId_jsonRPC();
                    GetPlantId();
                    double p = priceConvert(priceHex);
                    double actualprice = get_actual_price(plantId);
                    //Console.WriteLine($"Цена лота = {p}"); <-----
                    if (p * koef < actualprice && p > actualprice * down_limit && p < balance_PVU)
                    {
                        bid().Wait();
                        if (check_buy == "1")
                        {
                            Console.WriteLine($"Item in inventory! Count = {iter}");
                            if (saleOn == 1)
                            {
                                priceHex = BigInteger.Parse(Convert.ToString(Convert.ToInt32((get_actual_price(plantId) * (saleC)) * 1000000)) + "000000000000");
                                sale().Wait();
                            }
                        }
                    }
                    tokenId = "";
                }
                //Console.WriteLine("-----------------------\n\n"); //logs <-----
            }
        }

        Console.WriteLine("\n\n\nCOMPLITED");
        Console.ReadLine();
    }

    static async Task GetTransactionData_test()
    {
        var web3 = new Web3("https://bsc-dataseed.binance.org/");
        var info = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync("0xc6fcede3e675cbd2df2df022946a9212f6a8f6069d0f3d78de07cc709f4a2b60");
        Console.WriteLine((info.Input.ToString().Substring(0, 74)).Substring(64, 10));
    }

    static void GetTransactions_jsonRPC()
    {

        var web3 = new Web3(urlWeb3);

        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://api.bscscan.com/api?module=account&action=txlist&address=0x5ab19e7091dd208f352f8e727b6dcc6f8abb6275&startblock=11484560&page=1&offset=2&sort=desc&apikey=*********************************");

        webRequest.ContentType = "application/json";
        webRequest.Method = "Post";

        string json = @"
        {'status':'1','message':'OK','result':
        [{'blockNumber':'11488286','timeStamp':'1633378332','hash':'0xadffc14467228cafe983d90474cb79e7a0338cfdc728b830b2d5446c3e624075',
        'nonce':'110','blockHash':'0x4f2a58f0f10bbd9d8c014962daf9305ac9353941dfe70ac8f5c68747797e4a68','transactionIndex':'175',
        'from':'0x9880507e645f13a8ce44a87eb6a4a9de4eb1f701','to':'0x5ab19e7091dd208f352f8e727b6dcc6f8abb6275','value':'0','gas':'300000',
        'gasPrice':'5000000000','isError':'0','txreceipt_status':'1',
        'input':'0x3d7d3f5a000000000000000000000000000000000000000000000000000000000016ec66000000000000000000000000000000000000000000000001314fb37062980000000000000000000000000000000000000000000000000001314fb37062980000000000000000000000000000000000000000000000000000000001906ac69bf7',
        'contractAddress':'','cumulativeGasUsed':'15113438','gasUsed':'169600','confirmations':'2'}]}
        ";

        JObject joe = JObject.Parse(json);
        string s = JsonConvert.SerializeObject(joe);

        // serialize json for the request
        byte[] byteArray = Encoding.UTF8.GetBytes(s);
        webRequest.ContentLength = byteArray.Length;

        try
        {
            using (Stream dataStream = webRequest.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
        }
        catch (WebException we)
        {
            throw we;
        }

        WebResponse webResponse = null;
        try
        {
            using (webResponse = webRequest.GetResponse())
            {
                using (Stream str = webResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(str))
                    {
                        var t = JsonConvert.DeserializeObject<JObject>(sr.ReadToEnd());
                        Console.WriteLine(t);
                    }
                }
            }
        }
        catch (WebException webex)
        {
            using (Stream str = webex.Response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(str))
                {
                    var tempRet = JsonConvert.DeserializeObject<JObject>(sr.ReadToEnd());
                    var t = tempRet;
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    } //НЕ ИСПОЛЬЗУЕТСЯ

    static async Task GetInfo()
    {
        var c = count; //кол-во итераций
        var web3 = new Web3(urlWeb3);
        tokenId = ""; //обнуляем

        var info = await web3.Eth.Filters.NewPendingTransactionFilter.SendRequestAsync(); //1 ошибка (>20 сек)

        if (info != null)
        {

            string[] transHash = new string[10000];
            try
            {
                transHash = await web3.Eth.Filters.GetFilterChangesForBlockOrTransaction.SendRequestAsync(info);
                //Console.WriteLine($"Total transactions:  {transHash.Length.ToString()}\n"); //logs !
                if (transHash.Length < c)
                    c = transHash.Length;
            }
            catch
            {
                c = 0;
            }

            for (int i = 0; i < c; i++)
            {
                //check был до try
                try
                {
                    var check = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transHash[i].ToString());
                    if (check != null && check.To != null)
                        if (check.To.ToString() == "0x5ab19e7091dd208f352f8e727b6dcc6f8abb6275")
                        {
                            iter = i;
                            token_id64 = (check.Input.ToString().Substring(10, 64));
                            tokenIdTemp = new HexBigInteger(token_id64);
                            tokenIdHex = tokenIdTemp;
                            tokenId = token_id64.Substring(54, 10);
                            price = (check.Input.ToString().Substring(74, 64));
                            priceTemp = new HexBigInteger(price);
                            priceHex = priceTemp;
                            gas = check.Gas;
                            gasPrice = check.GasPrice;
                            i = c;
                            info_checker = true;
                            Console.WriteLine(tokenIdHex); //logs
                        }
                }
                catch (AggregateException e)
                {
                    //Console.WriteLine($"Error: {e}");
                    //throw;
                    info_checker = false;
                    //check = null;
                }
            }
        }
        else
        {
            info_checker = false;
        }
    }

    static void GetPlantId()
    {
        if (JO != null)
            if (JO["result"] != null)
            {
                plantId = Convert.ToInt64(JO["result"].Value<string>().Substring(120), 16);
                //Console.WriteLine($"Plant ID: {plantId}"); //logs <-----
            }
    }

    static void GetPlantId_jsonRPC()
    {
        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://bsc-mainnet.web3api.com/v1/**********************************");

        webRequest.ContentType = "application/json";
        webRequest.Method = "POST";

        string s1 = @"{""jsonrpc"":""2.0"",""id"":""1"",""method"":""eth_call"",""params"":[{""from"":""0x0000000000000000000000000000000000000000"",""data"":""0xed0b1f5f000000000000000000000000000000000000000000000000000000";
        string s2 = @""",""to"":""0x5ab19e7091dd208f352f8e727b6dcc6f8abb6275""},""latest""]}";

        byte[] byteArray = Encoding.UTF8.GetBytes(s1 + @tokenId + s2);
        webRequest.ContentLength = byteArray.Length;

        try
        {
            using (Stream dataStream = webRequest.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
        }
        catch (WebException we)
        {
            //Console.WriteLine("JSON - ex1"); //logs
            throw;
        }
        WebResponse webResponse = null;
        try
        {
            using (webResponse = webRequest.GetResponse())
            {
                using (Stream str = webResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(str))
                    {
                        //Console.WriteLine(sr.ReadToEnd()); //logs
                        JO = JsonConvert.DeserializeObject<JObject>(sr.ReadToEnd());
                    }
                }
            }
        }
        catch (WebException webex)
        {
            Console.WriteLine("JSON - ex2"); //logs
            using (Stream str = webex.Response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(str))
                {
                    var tempRet = JsonConvert.DeserializeObject<JObject>(sr.ReadToEnd());
                    JO = tempRet;
                }
            }
        }
        catch (Exception)
        {
            Console.WriteLine("JSON - ex3"); //logs
            throw;
        }
    }

    static double priceConvert(BigInteger value)
    {
        return (double)(value / 1000000000000000) / 1000.0;
    }

    static async Task bid()
    {
        var url = urlWeb3;
        var privateKey = ""; //вводим свой private key
        var account = new Account(privateKey);
        var web3 = new Web3(account, url);
        //var web3 = new Web3("https://bscscan.com/token/0x926eae99527a9503eadb4c9e927f21f84f20c977#writeContract");
        var bidHandler = web3.Eth.GetContractTransactionHandler<Bid>();
        var result = await bidHandler
            .SendRequestAndWaitForReceiptAsync(bidContractAddress, new Bid
            {
                bidTokenId = tokenIdHex,
                bidPrice = priceHex,
                Gas = gas,
                GasPrice = gasPrice,
            });
        Console.WriteLine("\nПокупаем...\n");
        check_buy = Convert.ToString(result.Status);
        Console.WriteLine($"Хэш транзакции: {result.TransactionHash}");
    }

    static async Task sale()
    {
        Console.WriteLine("\nПродаем...\n");
        var url = urlWeb3;
        var privateKey = ""; //вводим свой private key
        var account = new Account(privateKey);
        var web3 = new Web3(account, url);
        //var web3 = new Web3("https://bscscan.com/token/0x5ab19e7091dd208f352f8e727b6dcc6f8abb6275#writeContract");
        var bidHandler = web3.Eth.GetContractTransactionHandler<CreateSaleAuction>();
        var result = await bidHandler
            .SendRequestAndWaitForReceiptAsync(saleContractAddress, new CreateSaleAuction
            {
                saleTokenId = tokenIdHex,
                salePrice1 = priceHex,
                salePrice2 = priceHex,
                saleDuration = BigInteger.Parse("1719675249023"),
                Gas = gas,
                GasPrice = gasPrice,
            });
    }

    static async Task getBalance()
    {
        var url = urlWeb3;
        var privateKey = ""; //вводим свой private key
        var account = new Account(privateKey);
        var web3 = new Web3(account, url);
        //var web3 = new Web3("https://bscscan.com/token/0x31471e0791fcdbe82fbf4c44943255e923f1b794#readContract");
        //Console.WriteLine("\nПроверяем баланс...\n\n");
        var bidHandler = web3.Eth.GetContractQueryHandler<BalanceOf>();
        var result = await bidHandler
            .QueryRawAsync("0x31471e0791fcdbe82fbf4c44943255e923f1b794", new BalanceOf
            {
                acc = "", //вводим свой public key
            });
        balance = result.Substring(46, 20);
        HexBigInteger balanceTemp = new HexBigInteger(balance);
        BigInteger balance_bigInt = balanceTemp;
        balance_PVU = priceConvert(balance_bigInt);
    }

    static async Task selectData()
    {
        var config = Configuration.Default.WithDefaultLoader();
        var address = "https://pvu.isnot.dev/trand";
        var document = await BrowsingContext.New(config).OpenAsync(address);

        var data = document.QuerySelectorAll("tr > th");

        int pos = 5;

        for (int i = 0; i <= 8; i++) //8
            for (int j = 0; j <= 3; j++) //3
            {
                plant_price[i, j] = 0;
                int count = 0;
                string s = data[pos].Text();
                //Console.WriteLine($"{pos}-й: {data[pos].Text()}");

                while (s.Length > 0)
                {
                    char ch = '(';
                    int indexOfChar = s.IndexOf(ch);
                    plant_price[i, j] += Convert.ToDouble((s.Substring(0, indexOfChar)).Replace(".", ","));

                    ch = ' ';
                    indexOfChar = s.IndexOf(ch);
                    s = s.Substring(indexOfChar + 1, s.Length - indexOfChar - 1);
                    count++;
                }
                plant_price[i, j] = plant_price[i, j] / Convert.ToDouble(count);
                pos++;
            }
        //Console.WriteLine("Обновляем цены...\n"); <-----
    }

    static double get_actual_price(long _plant_id)
    {
        int x, y;
        string s = Convert.ToString(_plant_id);
        string type = s.Substring(3, 2);
        string rarity = s.Substring(5, 1);

        if (s.Substring(0, 1) == "2") // If mother-tree
        {
            if (type == "90")
                return 60;
            else if (type == "90")
                return 38;
            else if (type == "92")
                return 40;
            else if (type == "93")
                return 40;
        }

        if (rarity == "1") // common
            y = 0;
        else if (rarity == "2") //uncommon
            y = 1;
        else if (rarity == "3") //rare
            y = 2;
        else if (rarity == "4") //mythic
            y = 3;
        else
            return 0;

        if (type == "00" || type == "01" || type == "07" || type == "17" || type == "30") //fire
            x = 0;
        else if (type == "02" || type == "06" || type == "29") //ice
            x = 1;
        else if (type == "03" || type == "08" || type == "15" || type == "32" || type == "34") //electro
            x = 2;
        else if (type == "04" || type == "05" || type == "36" || type == "38" || type == "39") //water
            x = 3;
        else if (type == "09" || type == "10" || type == "16" || type == "37") //wind
            x = 4;
        else if (type == "11" || type == "12" || type == "13" || type == "22" || type == "23" || type == "24") //parasite
            x = 5;
        else if (type == "14" || type == "31" || type == "33" || type == "35") //dark
            x = 6;
        else if (type == "18" || type == "19" || type == "20" || type == "21") //light
            x = 7;
        else if (type == "25" || type == "26" || type == "27" || type == "28") //metal
            x = 8;
        else
            return 0;

        //Console.WriteLine($"Актуальная цена = {plant_price[x, y]}\n"); <------
        return plant_price[x, y];
    }

    static void data_from_file()
    {
        string xstr = "";
        // чтение из файла
        StreamReader f = new StreamReader("0prices.txt");

        for (int i = 0; i <= 8; i++) //8
        {
            xstr = f.ReadLine();
            for (int j = 0; j <= 3; j++) //3
            {
                plant_price[i, j] = Convert.ToDouble(f.ReadLine());
            }
        }
        f.Close();
    }

}

