## GLN语法描述
### 文档约定
- 文档使用 __=>__ 字符表示一个正则表达式，使用斜体描述不容易被正则语法直接表述的表达式。
- 文档使用 __:=__ 字符表示一个BNF范式，用来表达语法。
- 文档使用 __'…'__ 表示一个字符。
- 文档使用 __"…"__ 表示一个字符串。
- 文档使用 __=>>__ 表示等号前的表达式等价于一个sexpr。
- 文档使用小写字母 **a b c ……** 描述一个原子数据类型（Atom）。
- 文档使用大写字母 **A B C ……** 描述任意数据类型（Element）。
- 文档使用带有前缀'\`'的字母 **\`A \`B \`C ……** 描述不带有扩展语法的上下文（NonExElement）。

### 编码与存储
描述不对GLN的具体存储编码做出约定，通常应当使用UTF8（无BOM）作为文本默认编码。

### 数据类型
1. 字符: 字符的定义由具体实现决定，同时会影响词法解析对字符的处理。
2. 逻辑: 用于描述逻辑值，仅可取真(true)或者假(false)。
3. 整数: 用于描述一个有符号整数，具体取值范围由实现决定。
4. 小数: 用于描述一个小数，具体取值范围由实现决定。
5. 符号: 用于描述一个字符串，往往用于决定数据的语义。
6. 字符串: 用于描述一个由若干个字符构成的串。
7. 列表: 用于描述若干个数据的有序集合。

### 转义字符
GLN允许在字符串、字符或者符号中使用'\'作为转义字符，其后可接以下字符进行转义：

    转义序列     | 转义后        
    ------------ | ----------------------------------
    "\b"         | 退格符
    "\f"         | 换页符
    "\n"         | 换行符
    "\r"         | 回车符
    "\t"         | 制表符
    "\v"         | 垂直制表符
    "\u****"     | UCS2字符，其中\*表示十六进制数字

对于不在上表的转义序列将后接字符原样输出。若字符序列不足以构成转义序列则报错。

### 词法定义
#### 说明
- 注释以';'开头并持续到行尾，可以出现在行的任意位置。注释在解析后可以被解析器抛弃或者作为词法对象保存，由实现决定。
- 任意符号、数字或者保留字以空白、终结字符或者EOF作为结尾。
- 一旦字串以'#'开头则被视为一个保留字，当前实现中保留字仅允许为"#true"和"#false"，任何解析器无法识别的保留字会被识别为符号。
- 一个符号可以以'-'开头，但是'-'后不可接数字，一旦'-'后接数字将被识别为一个负数。
- 若数字结尾为空白、终结字符以外的字符则产生一个错误。

#### 词法元素表达式
- BlankCharacter => \[\t\v\r\n \]
- TerminalCharacter => \[\\\[\{()\}\\\];:\]
- Comment => ;.*$
- CharacterLiteral => ' _任意单个字符或单个字符的转义_ '
- StringLiteral => ' _任意多个字符或字符的转义_ '
- ReservedSymbolLiteral => #\[^TerminalCharacter\]*
- SymbolLiteral => _任意多个不为BlankCharacter或TerminalCharacter的字符或任意字符的转义，且不与ReservedSymbolLiteral和NumberLiteral冲突_
- NumberCharacter => \[0-9\]
- NonZeroNumberCharacter => \[1-9\]
- HexNumberCharacter => \[a-fA-F0-9\]
- NumberCharacterList := NumberCharacterList NumberCharacter | **ε**
- HexNumberCharacterList := HexNumberCharacterList HexNumberCharacter | **ε**
- HexIntegerLiteral := **0** **x** HexNumberCharacterList
- UnsignedIntegerLiteral := **0** | HexIntergerLiteral | NonZeroNumberCharacter NumberCharacterList
- IntegerLiteral := **-** UnsignedIntegerLiteral | UnsignedIntegerLiteral
- UnsignedIntegerPart := **0** | NonZeroNumberCharacter NumberCharacterList
- IntegerPart = **-** UnsignedIntegerPart | UnsignedIntegerPart
- FractionalPart := **.** NumberCharacterList
- UnsignedExponentPart := **0** | NonZeroNumberCharacter NumberCharacterList
- ExponentPart := **e** **-** UnsignedExponentPart | **e** UnsignedExponentPart
- RealLiteral := IntegerPart FractionalPart ExponentPart | IntegerPart FractionalPart | IntegerPart ExponentPart
- NumberLiteral := IntegerLiteral | RealLiteral

### 语法定义
#### 说明
- 用于描述语法的范式为空白不敏感的，即可以在构成语法元素的终结符号之间插入任意多个BlankCharacter

#### 语法表达式
- Atom := CharacterLiteral | StringLiteral | ReservedSymbolLiteral | SymbolLiteral | NumberLiteral
- Element := Atom | SList | TList | ExList
- NonExElement := Atom | SList | TList
- ElementList := ElementList Element | **ε**
- SList := **\[** ElementList **\]**
- TList := Element **(** ElementList **)**
- ExList := Element **:** NonExElement | Element **:** NonExElement **{** ElementList **}** | Element **{** ElementList **}**

#### 语义
- Atom: 用于描述一个最小的数据单位，对应除列表以外的其他基础数据类型。
- SList: 描述一个含有若干个元素的或为空的列表，如：\[A B C\] =>> (A B C)。
- TList: 描述一个含有若干个元素的不为空的列表，如：A(B C) =>> (A B C)。
- ExList：定义一个列表扩展语法，见下文。

##### 列表扩展语法
列表扩展语法作为语法糖，用于描述一个列表的后续元素。
对于前置元素若不为列表则将其提升为一个仅含有该元素的列表。
具体形式如下：
###### 1. A : B
若A为列表，则B作为元素加入A列表的末尾，如：(A B) : C =>> (A B C)。
若A不为列表，则将A提升为一个仅含有A的列表并将B加入末尾，如：a : B =>> (a B)。
###### 2. A { B }
若A为列表，则B作为元素加入A列表的末尾，如：(A B) { C } =>> (A B C)。
若A不为列表，则将A提升为一个仅含有A的列表并将B加入末尾，如：a { B } =>> (a B)。
###### 3. A : \`B { C }
若A为列表，则B和C作为元素加入A列表的末尾，如：(A) : B { C } =>> (A B C)。
若A不为列表，则将A提升为一个仅含有A的列表并将B和C加入末尾，如：a : B { C } =>> (a B C)。
其中\`B为不含ExList语法的上下文，此举用于防止如下数据：
        a : b { c d e }
被解析为
        (a (b c d e))
而不是
        (a b c d e)
