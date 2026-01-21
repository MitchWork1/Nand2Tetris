using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;

public class Program
{
    static void Main(string[] args)
    {
        string inDir = "C:\\Users\\mitch\\source\\repos\\MitchWork1\\Nand2Tetris\\Nand2Tetris\\StaticTest";
        string inAsm = "C:\\Users\\mitch\\source\\repos\\MitchWork1\\Nand2Tetris\\Nand2Tetris\\FibTest2\\FibTest2.asm";
        string outDir = "C:\\Users\\mitch\\source\\repos\\MitchWork1\\Nand2Tetris\\Nand2Tetris\\FibTest2\\FibTest2.hack";
        VMTranslator translator = new VMTranslator(inDir, inDir);
        HackAssembler ha = new HackAssembler(inAsm, outDir);
    }

}


public class Parser
{
    StreamReader reader;
    public string currentInstruction = "";
    string A_INSTRUCTION = "A_INSTRUCTION";
    string L_INSTRUCTION = "L_INSTRUCTION";
    string C_INSTRUCTION = "C_INSTRUCTION";

    public Parser(string fileDirectory)
    {
        reader = new StreamReader(fileDirectory);
    }

    public bool hasMoreLines()
    {
        if (reader.Peek() != -1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void advance()
    {
        currentInstruction = null;
        while (hasMoreLines())
        {
            string line = reader.ReadLine();
            if (line == null) break;

            line = line.Trim();
            if (line == "" || line.StartsWith("//")) continue;

            int commentIndex = line.IndexOf("//");
            if (commentIndex != -1)
                line = line[..commentIndex].Trim();

            if (line != "")
            {
                currentInstruction = line;
                break;
            }
        }
    }

    public string instructionType()
    {
        if (currentInstruction[0] == '@') { return A_INSTRUCTION; }
        if (currentInstruction.StartsWith("(") && currentInstruction.EndsWith(")"))
            return L_INSTRUCTION;
        else { return C_INSTRUCTION; }
    }

    public string symbol()
    {
        if (instructionType() == A_INSTRUCTION)
            return currentInstruction[1..];
        if (instructionType() == L_INSTRUCTION)
            return currentInstruction[1..^1];
        return "null";
    }

    public string dest()
    {
        int index = currentInstruction.IndexOf('=');
        if (index == -1) { return "null"; }
        return currentInstruction[..index];
    }

    public string comp()
    {
        int eq = currentInstruction.IndexOf('=');
        int sm = currentInstruction.IndexOf(';');
        int start = eq == -1 ? 0 : eq + 1;
        int end = sm == -1 ? currentInstruction.Length : sm;
        return currentInstruction[start..end];
    }

    public string jump()
    {
        int index = currentInstruction.IndexOf(';');
        if (index == -1) { return "null"; }
        index += 1;
        return currentInstruction[index..];
    }

    public void terminal()
    {
        while (true)
        {
            Console.WriteLine($"Line: {currentInstruction}");
            Console.WriteLine($"Funtions:\n1. Advance\n2. Instruction Type\n3. Symbol\n4. dest\n5. comp\n6. jump");
            string ans = Console.ReadLine();
            switch (ans)
            {
                case "1":
                    advance();
                    break;
                case "2":
                    Console.WriteLine($"Instruction type: {instructionType()}");
                    break;
                case "3":
                    Console.WriteLine($"Symbol: {symbol()}");
                    break;
                case "4":
                    Console.WriteLine($"Dest: {dest()}");
                    break;
                case "5":
                    Console.WriteLine($"comp: {comp()}");
                    break;
                case "6":
                    Console.WriteLine($"Jump: {jump()}");
                    break;
                default:
                    return;
            }
        }
    }
}

public class Code
{
    public string dest(string dest)
    {
        string bi = "";
        if(dest == "null") { return "000"; }
        if (dest.Contains("A")) { bi += '1'; } else { bi += '0'; }
        if (dest.Contains("D")) { bi += '1'; } else { bi += '0'; }
        if (dest.Contains("M")) { bi += '1'; } else { bi += '0'; }          
        return bi;
    }
    public string comp(string comp)
    {
        switch (comp) 
        {
            case "0":
                return "0101010";
            case "1":
                return "0111111";
            case "-1":
                return "0111010";
            case "D":
                return "0001100";
            case "A":
                return "0110000";
            case "M":
                return "1110000";
            case "!D":
                return "0001101";
            case "!A":
                return "0110001";
            case "!M":
                return "1110001";
            case "-D":
                return "0001111";
            case "-A":
                return "1001111";
            case "-M":
                return "1110011";
            case "D+1":
                return "0011111";
            case "A+1":
                return "0110111";
            case "M+1":
                return "1110111";
            case "D-1":
                return "0001110";
            case "A-1":
                return "0110010";
            case "M-1":
                return "1110010";
            case "D+A":
                return "0000010";
            case "D+M":
                return "1000010";
            case "D-A":
                return "0010011";
            case "D-M":
                return "1010011";
            case "A-D":
                return "0000111";
            case "M-D":
                return "1000111";
            case "D&A":
                return "0000000";
            case "D&M":
                return "1000000";
            case "D|A":
                return "0010101";
            case "D|M":
                return "1010101";
            default:
                return "null";
        }
    }
    public string jump(string jump)
    {
        switch (jump)
        {
            case "JGT":
                return "001";
            case "JEQ":
                return "010";
            case "JGE":
                return "011";
            case "JLT":
                return "100";
            case "JNE":
                return "101";
            case "JLE":
                return "110";
            case "JMP":
                return "111";
            default:
                return "000";
        }
    }

}

public class HackAssembler
{
    public Parser parser1 { get; set; }
    public Parser parser2 { get; set; }
    public Code code { get; set; }
    public SymbolTable symbolTable { get; set; }
    public HackAssembler(string desFileIn, string desFileOut)
    {
        StreamWriter writer = new StreamWriter(desFileOut);
        parser1 = new Parser(desFileIn);
        parser2 = new Parser(desFileIn);
        code = new Code();
        symbolTable = new SymbolTable();
        int nextAvailableAddressRAM = 16;

        int romInt = 0;
        while (parser1.hasMoreLines())
        {
            parser1.advance();
            if(parser1.instructionType() == "A_INSTRUCTION")
            {
                romInt++;
            }
            if (parser1.instructionType() == "C_INSTRUCTION")
            {
                romInt++;
            }
            if (parser1.instructionType() == "L_INSTRUCTION")
            {
                symbolTable.addEntry(parser1.symbol(), romInt);
            }
        }
        while (parser2.hasMoreLines())
        {
            parser2.advance();

            if (string.IsNullOrEmpty(parser2.currentInstruction))
                continue;

            if (parser2.instructionType() == "A_INSTRUCTION")
            {
                int result;
                if (int.TryParse(parser2.symbol(), out result))
                {
                    writer.WriteLine("0" + Convert.ToString(result, 2).PadLeft(15, '0'));                   
                }
                else
                {
                    if (symbolTable.contains(parser2.symbol()))
                    {
                        writer.WriteLine("0" + Convert.ToString(symbolTable.getAddress(parser2.symbol()), 2).PadLeft(15, '0'));
                    }
                    else
                    {
                        symbolTable.addEntry(parser2.symbol(), nextAvailableAddressRAM);                        
                        writer.WriteLine("0" + Convert.ToString(symbolTable.getAddress(parser2.symbol()), 2).PadLeft(15, '0'));
                        nextAvailableAddressRAM++;
                    }
                }
            }
            if (parser2.instructionType() == "C_INSTRUCTION")
            {
                string bi = "111";
                bi += code.comp(parser2.comp());
                bi += code.dest(parser2.dest());
                bi += code.jump(parser2.jump());
                writer.WriteLine(bi);
            }
        }

        writer.Close();
        string text = File.ReadAllText(desFileOut);
        text = text.TrimEnd('\r', '\n');
        File.WriteAllText(desFileOut, text);
    }
}

public class SymbolTable
{
    Dictionary<string, int> keyValuePairs = new Dictionary<string, int>{
        {"R0", 0 },
        {"R1", 1 },
        {"R2", 2 },
        {"R3", 3 },
        {"R4", 4 },
        {"R5", 5 },
        {"R6", 6 },
        {"R7", 7 },
        {"R8", 8 },
        {"R9", 9 },
        {"R10", 10 },
        {"R11", 11 },
        {"R12", 12 },
        {"R13", 13 },
        {"R14", 14 },
        {"R15", 15 },
        {"SP", 0 },
        {"LCL", 1 },
        {"ARG", 2 },
        {"THIS", 3 },
        {"THAT", 4 },
        {"SCREEN", 16384 },
        {"KBD", 24576 },
    };
    public void addEntry(string symbol, int addres)
    {
        keyValuePairs.Add(symbol, addres);
    }
    public bool contains(string symbol)
    {
        if(keyValuePairs.ContainsKey(symbol)) { return  true; }
        else {  return false; }
    }
    public int getAddress(string symbol)
    {
        return keyValuePairs[symbol];
    }
}



public class VMParser
{
    StreamReader reader;
    string currentInstruction;

    public VMParser(string inDir)
    {
        reader = new StreamReader(inDir);
    }

    public bool hasMoreLines()
    {
        return !reader.EndOfStream;
    }

    public void advance()
    {
        currentInstruction = null;

        while (hasMoreLines())
        {
            string line = reader.ReadLine();

            if (line == null) break;

            if (line.Contains("//"))
                line = line[..line.IndexOf("//")];

            line = line.Trim();

            if (line.Length > 0)
            {
                currentInstruction = line;
                return;
            }
        }
    }

    public string commandType()
    {
        if (currentInstruction.StartsWith("push")) return "C_PUSH";
        if (currentInstruction.StartsWith("pop")) return "C_POP";

        if (currentInstruction == "add" ||
            currentInstruction == "sub" ||
            currentInstruction == "neg" ||
            currentInstruction == "eq" ||
            currentInstruction == "gt" ||
            currentInstruction == "lt" ||
            currentInstruction == "and" ||
            currentInstruction == "or" ||
            currentInstruction == "not")
            return "C_ARITHMETIC";

        if (currentInstruction.StartsWith("label")) return "C_LABEL";
        if (currentInstruction.StartsWith("goto")) return "C_GOTO";
        if (currentInstruction.StartsWith("if-goto")) return "C_IF";
        if (currentInstruction.StartsWith("function")) return "C_FUNCTION";
        if (currentInstruction.StartsWith("call")) return "C_CALL";
        if (currentInstruction.StartsWith("return")) return "C_RETURN";

        throw new Exception("Unknown command: " + currentInstruction);
    }


    public string arg1()
    {
        string type = commandType();

        if (type == "C_RETURN")
            throw new Exception("arg1() called on C_RETURN");
        if (type == "C_ARITHMETIC")
            return currentInstruction;
        return currentInstruction.Split(' ')[1];
    }

    public int arg2()
    {
        string type = commandType();
        if (type == "C_PUSH" ||
            type == "C_POP" ||
            type == "C_FUNCTION" ||
            type == "C_CALL")
            return int.Parse(currentInstruction.Split(' ')[2]);

        throw new Exception("arg2() not valid for command: " + type);
    }
}

public class VMCodeWriter
{
    StreamWriter writer;
    string fileName;
    int compCounter = 0;
    int functionCounter = 0;
    string dir;
    string currentFunction = "";
    public VMCodeWriter(string outDir)
    {
        dir = outDir;
        writer = new StreamWriter(outDir);
    }

    public void initSp()
    {
        writer.WriteLine("//Init SP");
        writer.WriteLine("@256");
        writer.WriteLine("D=A");
        writer.WriteLine("@SP");
        writer.WriteLine("M=D");
        currentFunction = "Sys";
        writeCall("Sys.init", 0);
        writer.WriteLine();
    }

    public void setFileName(string name)
    {
        fileName = name;
    }

    public void writeArithmetic(string command)
    {
        writer.WriteLine("//" + command);
        switch (command)
        {
            case "add":
                doOperatorToD("+");
                pushD();
                break;
            case "sub":
                doOperatorToD("-");
                pushD();
                break;
            case "neg":
                writer.WriteLine("@SP");
                writer.WriteLine("A=M-1");
                writer.WriteLine("M=-M");
                break;
            case "eq":
                doOperatorToD("-");
                compDJumpAndPushDBool("JEQ");
                break;
            case "gt":
                doOperatorToD("-"); //x - y
                compDJumpAndPushDBool("JGT");
                break;
            case "lt":
                doOperatorToD("-"); //x - y
                compDJumpAndPushDBool("JLT");
                break;
            case "and":
                doOperatorToD("&"); //x&y
                pushD();
                break;
            case "or":
                doOperatorToD("|"); //x|y
                pushD();
                break;
            case "not": //Not y / !y
                writer.WriteLine("@SP");
                writer.WriteLine("A=M-1");
                writer.WriteLine("M=!M");
                break;
        }
        writer.WriteLine();
    }

    public void writePushPop(string command, string segment, int index)
    {
        writer.WriteLine("//" + command + " " + segment + " " + index);
        if (command == "C_PUSH")
        {
            switch (segment)
            {
                case "constant":
                    writer.WriteLine($"@{index}");
                    writer.WriteLine("D=A");
                    pushD();
                    break;
                case "local":
                    segmentValueToD(index, "LCL");
                    pushD();
                    break;
                case "argument":
                    segmentValueToD(index, "ARG");
                    pushD();
                    break;
                case "this":
                    segmentValueToD(index, "THIS");
                    pushD();
                    break;
                case "that":
                    segmentValueToD(index, "THAT");
                    pushD();
                    break;
                case "temp":
                    writer.WriteLine($"@5");  // base of temp
                    writer.WriteLine("D=A");  // D = 5
                    writer.WriteLine($"@{index}");
                    writer.WriteLine("A=D+A"); // A = 5 + index
                    writer.WriteLine("D=M");    // D = RAM[5 + index]
                    pushD();
                    break;
                case "pointer":
                    if (index == 0) { writer.WriteLine("@THIS"); writer.WriteLine("D=M"); pushD(); }
                    else { writer.WriteLine("@THAT"); writer.WriteLine("D=M"); pushD(); }
                    break;
                case "static":
                    writer.WriteLine($"@{fileName}.{index}");
                    writer.WriteLine("D=M");
                    pushD();
                    break;
            }

        }
        if (command == "C_POP")
        {
            switch (segment)
            {
                case "local":
                    storeDInSegment(index, "LCL");
                    break;
                case "argument":
                    storeDInSegment(index, "ARG");
                    break;
                case "this":
                    storeDInSegment(index, "THIS");
                    break;
                case "that":
                    storeDInSegment(index, "THAT");
                    break;
                case "temp":
                    popToD();              
                    writer.WriteLine($"@{5 + index}");
                    writer.WriteLine("M=D");
                    break;
                case "pointer":
                    popToD();
                    if (index == 0) { writer.WriteLine("@THIS"); writer.WriteLine("M=D"); }
                    else { writer.WriteLine("@THAT"); writer.WriteLine("M=D"); }
                    break;
                case "static":
                    popToD();
                    writer.WriteLine($"@{fileName}.{index}");
                    writer.WriteLine("M=D");
                    break;
            }
        }
        writer.WriteLine();
    }

    public void writeLabel(string labelName)
    {
        writer.WriteLine("//writeLabel Start");
        string owner = string.IsNullOrEmpty(currentFunction) ? fileName : currentFunction;
        writer.WriteLine($"({owner}${labelName})");
        writer.WriteLine();
    }

    public void writeGoto(string jumpPoint)
    {
        writer.WriteLine("//writeGo Start");
        string owner = string.IsNullOrEmpty(currentFunction) ? fileName : currentFunction;
        writer.WriteLine($"@{owner}${jumpPoint}");
        writer.WriteLine("0;JMP");
        writer.WriteLine("");
    }

    public void writeIf(string jumpPoint)
    {
        writer.WriteLine("//writeIf Start");
        popToD();
        string owner = string.IsNullOrEmpty(currentFunction) ? fileName : currentFunction;
        writer.WriteLine($"@{owner}${jumpPoint}");
        writer.WriteLine("D;JNE");
        writer.WriteLine("");
    }

    public void writeFunction(string functionName, int nVars)
    {
        currentFunction = functionName;
        writer.WriteLine("//writeFunction Start");
        writer.WriteLine($"({functionName})");
        if (nVars > 0)
        {
            writer.WriteLine("@0");
            writer.WriteLine("D=A");
            for (int i = 0; i < nVars; i++) //Initialise variables
            {
                pushD();
            }
        }
        writer.WriteLine("");
    }

    public void writeCall(string functionName, int nArgs)
    {
        writer.WriteLine("//writeCall Start");
        string returnAddress = pushReturnAddress();
        pushFrame();
        repositionLCLAndARGAfterFrame(nArgs);
        writer.WriteLine($"@{functionName}");
        writer.WriteLine("0;JMP");
        writer.WriteLine($"({returnAddress})");
        writer.WriteLine("");
    }

    public void writeReturn()
    {
        writer.WriteLine("//writeReturn Start");
        writer.WriteLine("@LCL"); //LCL is pointer 
        writer.WriteLine("D=M"); //D = Value at pointer ie an address (261)
        writer.WriteLine("@R13");
        writer.WriteLine("M=D"); //Frame RAM[R13] = 261

        writer.WriteLine("@R13");
        writer.WriteLine("D=M"); //Frame D=261
        writer.WriteLine("@5");
        writer.WriteLine("A=D-A"); //A 256 = 261  - 5 ...Return address stored at 256
        writer.WriteLine("D=M"); //D = return address
        writer.WriteLine("@R14");
        writer.WriteLine("M=D");  //return address

        popToD(); //Popping return value off of the stack
        writer.WriteLine("@ARG"); //254 = arg0
        writer.WriteLine("A=M"); //set a to value in pointer
        writer.WriteLine("M=D"); //Value at pointer equals return address

        writer.WriteLine("@ARG"); //Set SP=ARG + 1
        writer.WriteLine("D=M+1");
        writer.WriteLine("@SP");
        writer.WriteLine("M=D");

        restoreSegment("THAT", 1);
        restoreSegment("THIS", 2);
        restoreSegment("ARG", 3);
        restoreSegment("LCL", 4);

        writer.WriteLine("@R14");
        writer.WriteLine("A=M");
        writer.WriteLine("0;JMP");
        writer.WriteLine("");
    }

    public void close() //close writer and put asm in infinite loop
    {
        writer.WriteLine("//close start");
        writer.WriteLine("@END");
        writer.WriteLine("0;JMP");
        writer.Close();
        
    }

    #region Helper Functions

    public void pushD() //used to push value in D onto stack
    {
        writer.WriteLine("@SP"); //A = SP - Get address
        writer.WriteLine("A=M"); //Set A = SPval - get value in SP  => Value in SP becomes address
        writer.WriteLine("M=D"); // -Go to address and set the value to D
        writer.WriteLine("@SP"); //A = SP
        writer.WriteLine("M=M+1"); //SPval++
    }

    public void popToD() //used to pop value at top of stack into D
    {
        writer.WriteLine("@SP"); //A= address of SP
        writer.WriteLine("M=M-1"); //(SPval - 1) M is top of stack
        writer.WriteLine("@SP"); //a=address of SP
        writer.WriteLine("A=M"); // A = val in Sp
        writer.WriteLine("D=M"); // D = val in SP
    }

    public void segmentValueToD(int index, string RAMName) //Sets D = the value at (segment + index)
    {
        writer.WriteLine($"@{index}"); //A=index
        writer.WriteLine("D=A"); //D=index
        writer.WriteLine($"@{RAMName}"); //Address of local
        writer.WriteLine("A=D+M"); //D = Value at local + index (D)
        writer.WriteLine("D=M"); //D = Value at local + index (D)
    }

    public void indexPlusSegmentToD(int index, string RAMName) //Set D = segment + index (An address)
    {
        writer.WriteLine($"@{index}"); //A=index
        writer.WriteLine("D=A"); //D=index
        writer.WriteLine($"@{RAMName}"); //Address of local
        writer.WriteLine("D=D+M"); //D = Value at local + index (D)
    }

    public void storeDInSegment(int index, string RAMName)
    {
        indexPlusSegmentToD(index, RAMName);
        popDToRNum();
        popToD();
        writer.WriteLine("@R13"); //A = address of temp
        writer.WriteLine("A=M"); //A = value in temp
        writer.WriteLine("M=D"); //Store D in value of address stored in temp
    }

    public void doOperatorToD(string op) // x op y examples: x - y / x&y
    {
        popDToRNum(); //y
        popToD(); //x
        writer.WriteLine("@R13");
        writer.WriteLine($"D=D{op}M");
    }

    public void popDToRNum(int num=13)
    {
        popToD();
        writer.WriteLine($"@R{num}");
        writer.WriteLine("M=D");
    }

    public void compDJumpAndPushDBool(string jmpCondition) //Push bool awnser to stack of D Jmpcomparison e.g. D = 0 JEQ => saves true to stack
    {
        int id = compCounter++;
        writer.WriteLine($"@COMPARISON{jmpCondition}{id}");
        writer.WriteLine($"D;{jmpCondition}");
        writer.WriteLine($"D=0");
        writer.WriteLine($"@END{jmpCondition}{id}");
        writer.WriteLine("0;JMP");
        writer.WriteLine($"(COMPARISON{jmpCondition}{id})"); //If comparison e.g.x eq y is true 
        writer.WriteLine("D=-1");
        writer.WriteLine($"@END{jmpCondition}{id}");
        writer.WriteLine("0;JMP");
        writer.WriteLine($"(END{jmpCondition}{id})");
        pushD();

    }

    public void pushMFromA(string address) //pushes value of A (M) onto stack
    {
        writer.WriteLine($"@{address}");
        writer.WriteLine("D=M");
        pushD();
    }

    public string pushReturnAddress() //Injects the return address my making a label
    {
        string owner = string.IsNullOrEmpty(currentFunction) ? fileName : currentFunction;
        string returnLabel = $"{owner}$ret.{functionCounter++}";
        writer.WriteLine($"@{returnLabel}");
        writer.WriteLine("D=A");
        pushD();
        return returnLabel;
    }

    public void pushFrame()
    {
        pushMFromA("LCL");
        pushMFromA("ARG");
        pushMFromA("THIS");
        pushMFromA("THAT");
    }

    public void repositionLCLAndARGAfterFrame(int nArgs)
    {

        /*
            Arg 0 254
            Arg 1 255
            RETURN ADDRESS 256
            SAVED LCL 257
            SAVED ARG 258
            SAVED THIS 259
            SAVED THAT 260
            LCL 261 (SP pointing to 261)
         */
        writer.WriteLine("@SP");
        writer.WriteLine("D=M"); //D = address of SP (261)
        writer.WriteLine($"@{nArgs + 5}"); //Lets say 2 args
        writer.WriteLine("D=D-A"); //Then 254 = 261 - 7
        writer.WriteLine("@ARG");
        writer.WriteLine("M=D");


        writer.WriteLine("@SP"); //SP pointing to lcl segment of function 261
        writer.WriteLine("D=M"); 
        writer.WriteLine("@LCL");
        writer.WriteLine("M=D"); //261
    }

    private void restoreSegment(string segment, int offset)
    {
        writer.WriteLine("@R13");  // FRAME
        writer.WriteLine("D=M");
        writer.WriteLine($"@{offset}");
        writer.WriteLine("A=D-A");  // A = FRAME - offset
        writer.WriteLine("D=M");    // D = *(FRAME - offset)
        writer.WriteLine($"@{segment}");
        writer.WriteLine("M=D");
    }
    #endregion
}

public class VMTranslator
{
    VMParser parser;
    VMCodeWriter codeWriter;
    string dir;

    public VMTranslator(string inDir, string outDir)
    {
        dir = outDir;

        if (Directory.Exists(inDir))
        {
            string[] vmFiles = Directory.GetFiles(inDir, "*.vm");
            string outFileName = Path.Combine(outDir, Path.GetFileName(inDir) + ".asm");
            codeWriter = new VMCodeWriter(outFileName);
            codeWriter.initSp();
            foreach (string vmFile in vmFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(vmFile);

                parser = new VMParser(vmFile);
                codeWriter.setFileName(fileName);
                translateFile();
            }

            codeWriter.close();
        }
    }

    private void translateFile()
    {
        while (parser.hasMoreLines())
        {
            parser.advance();
            switch (parser.commandType())
            {
                case "C_PUSH":
                    codeWriter.writePushPop(parser.commandType(), parser.arg1(), parser.arg2());
                    break;
                case "C_POP":
                    codeWriter.writePushPop(parser.commandType(), parser.arg1(), parser.arg2());
                    break;
                case "C_ARITHMETIC":
                    codeWriter.writeArithmetic(parser.arg1());
                    break;
                case "C_LABEL":
                    codeWriter.writeLabel(parser.arg1());
                    break;
                case "C_GOTO":
                    codeWriter.writeGoto(parser.arg1());
                    break;
                case "C_IF":
                    codeWriter.writeIf(parser.arg1());
                    break;
                case "C_FUNCTION":
                    codeWriter.writeFunction(parser.arg1(), parser.arg2());
                    break;
                case "C_CALL":
                    codeWriter.writeCall(parser.arg1(), parser.arg2());
                    break;
                case "C_RETURN":
                    codeWriter.writeReturn();
                    break;
            }
        }
    }
}

public class JackTonenizer
{
    StreamReader reader;
    string[] currentTokens;
    string currentToken;
    int iToken = 0;
    bool comment = false;
    string[] keywords = {"class", "constructor", "function", "method", "field", "static", "var",
        "int", "char", "boolean", "void", "true", "false", "null", "this", "let", "do", "if", "else",
        "while", "return"};
    string[] symbols = { "{", "}", "(", ")", "[", "]", ".", ",", ";", "+", "-", "*", "/", "&", "|", "<", ">", "=", "~" };

    bool startOfComment = false;
    public JackTonenizer(string inDir)
    {
        reader = new StreamReader(inDir);
    }

    public bool hasMoreTokens()
    {
        return !reader.EndOfStream;
    }

    public void advance()
    {
        while (true)
        {
            if (currentTokens == null)
            {
                if (hasMoreTokens())
                {
                    currentTokens = reader.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    iToken = 0;
                }
            }
            if (iToken < currentTokens.Length)
            {
                currentToken = currentTokens[iToken];
                iToken++;
                if (comment) { if (!currentToken.StartsWith("*/")) { continue; } else { comment = false; return; } }
                else if (currentToken.StartsWith("//")) { iToken = currentTokens.Length; continue; }
                else if (currentToken.StartsWith("/*")) { comment = true; continue; }
                return;
            }
            else
            {
                if (hasMoreTokens())
                {
                    currentTokens = reader.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    iToken = 0;
                }
                else
                {
                    return;
                }
            }
        }
    }

    public string tokenType()
    {

        if (keywords.Contains(currentToken))
        {
            return "KEYWORD";
        }
        else if (symbols.Contains(currentToken))
        {
            return "SYMBOL";
        }
        else if (currentToken.StartsWith("\"") && currentToken.EndsWith("\""))
        {
            return "STRING_CONST";
        }
        else if (int.TryParse(currentToken, out int value) && value >= 0 && value <= 32767)
        {
            return "INT_CONST";
        }
        else if (char.IsLetter(currentToken[0]) || currentToken[0] == '_')
        {
            return "IDENTIFIER";
        }
        else
        {
            return "NULL";
        }
    }

    public string keyWord()
    {
        
    }
    


}

