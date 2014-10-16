## libgln

GLN(General purpose List-based Notation)是一种轻量级的数据描述格式，以列表作为唯一一种数据结构用于数据存储与交换。

libgln实现一套基于这种语法的解析库。

### GLN语法

e.g:
```
        shop {
            bookshelf : [lastAccess:"10 days ago"] {
                book(name:"The CXX Programming Language" price:450.0)
            }
        }
```
相当于XML形式的：
```xml
        <shop>
            <bookshelf lastAccess="10 days ago">
                <book name="The CXX Programming Language" price="450.0" />
            </bookshelf>
        </shop>
```
等价于s-expr的：
```lisp
        (shop
            (bookshelf
                ( (lastAccess "10 days ago") )
                (book (name "The CXX Programming Language") (price 450.0))
            )
        )
```

详细语法定义见 glnsyntax.md

### 编译
#### VisualStudio环境

- 打开libgln.sln即可（需要VisualStudio2013或更高版本，使用C++11标准/.net 4.0）

### 许可

本项目基于MIT许可，详细信息见LICENSE
