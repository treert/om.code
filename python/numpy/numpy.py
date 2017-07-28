'''
numpy用于处理矩阵运算，比python的列表好并不少。基本类ndarray
需要额外安装
# 使用 python 3+:
pip3 install numpy

# 使用 python 2+:
pip install numpy

> https://morvanzhou.github.io
'''

import numpy as np #为了方便使用numpy 采用np简写

##### 属性

array = np.array([[1,2,3],[2,3,4]])  #列表转化为矩阵
print(array)
"""
array([[1, 2, 3],
       [2, 3, 4]])
"""

print('number of dim:',array.ndim)  # 维度
# number of dim: 2

print('shape :',array.shape)    # 行数和列数
# shape : (2, 3)

print('size:',array.size)   # 元素个数
# size: 6

print(array.tolist()) # 矩阵转化为python列表
'''
[[1, 2, 3], [2, 3, 4]]
'''

##### 创建
a = np.array([2,23,4])  # list 1d
print(a)
# [2 23 4]

a = np.array([2,23,4],dtype=np.int)
print(a.dtype)
# int 64

a = np.array([2,23,4],dtype=np.int32)
print(a.dtype)
# int32

a = np.array([2,23,4],dtype=np.float32)
print(a.dtype)
# float32

a = np.array([2,23,4],dtype=np.float32)
print(a.dtype)
# float32

a = np.array([[2,23,4],[2,32,4]])  # 2d 矩阵 2行3列
print(a)
"""
[[ 2 23  4]
 [ 2 32  4]]
"""

a = np.zeros((3,4)) # 数据全为0，3行4列
"""
array([[ 0.,  0.,  0.,  0.],
       [ 0.,  0.,  0.,  0.],
       [ 0.,  0.,  0.,  0.]])
"""

a = np.ones((3,4),dtype = np.int)   # 数据为1，3行4列
"""
array([[1, 1, 1, 1],
       [1, 1, 1, 1],
       [1, 1, 1, 1]])
"""

a = np.empty((3,4)) # 数据为empty，3行4列
"""
array([[  0.00000000e+000,   4.94065646e-324,   9.88131292e-324,
          1.48219694e-323],
       [  1.97626258e-323,   2.47032823e-323,   2.96439388e-323,
          3.45845952e-323],
       [  3.95252517e-323,   4.44659081e-323,   4.94065646e-323,
          5.43472210e-323]])
"""

a = np.arange(10,20,2) # 10-19 的数据，2步长
"""
array([10, 12, 14, 16, 18])
"""

a = np.arange(12).reshape((3,4))    # 3行4列，0到11
"""
array([[ 0,  1,  2,  3],
       [ 4,  5,  6,  7],
       [ 8,  9, 10, 11]])
"""

a = np.linspace(1,10,20)    # 开始端1，结束端10，且分割成20个数据，生成线段
"""
array([  1.        ,   1.47368421,   1.94736842,   2.42105263,
         2.89473684,   3.36842105,   3.84210526,   4.31578947,
         4.78947368,   5.26315789,   5.73684211,   6.21052632,
         6.68421053,   7.15789474,   7.63157895,   8.10526316,
         8.57894737,   9.05263158,   9.52631579,  10.        ])
"""

a = np.linspace(1,10,20).reshape((5,4)) # 更改shape
"""
array([[  1.        ,   1.47368421,   1.94736842,   2.42105263],
       [  2.89473684,   3.36842105,   3.84210526,   4.31578947],
       [  4.78947368,   5.26315789,   5.73684211,   6.21052632],
       [  6.68421053,   7.15789474,   7.63157895,   8.10526316],
       [  8.57894737,   9.05263158,   9.52631579,  10.        ]])
"""

##### 运算

a=np.array([10,20,30,40])   # array([10, 20, 30, 40])
b=np.arange(4)              # array([0, 1, 2, 3])

c=a-b   # array([10, 19, 28, 37])
c=a*b   # array([  0,  20,  60, 120])
c=b**2  # array([0, 1, 4, 9])

c=10*np.sin(a)  
# array([-5.44021111,  9.12945251, -9.88031624,  7.4511316 ])

print(b<3)  
# array([ True,  True,  True, False], dtype=bool)

### 多维数组运算
a=np.array([[1,1],[0,1]])
b=np.arange(4).reshape((2,2))

print(a)
# array([[1, 1],
#       [0, 1]])

print(b)
# array([[0, 1],
#       [2, 3]])

c_dot = np.dot(a,b)
# array([[2, 4],
#       [2, 3]])
c_dot_2 = a.dot(b)
# array([[2, 4],
#       [2, 3]])

### 运算 & 降维
a=np.random.random((2,4))
print(a)
# array([[ 0.94692159,  0.20821798,  0.35339414,  0.2805278 ],
#       [ 0.04836775,  0.04023552,  0.44091941,  0.21665268]])
np.sum(a)   # 4.4043622002745959
np.min(a)   # 0.23651223533671784
np.max(a)   # 0.90438450240606416

print("a =",a)
# a = [[ 0.23651224  0.41900661  0.84869417  0.46456022]
# [ 0.60771087  0.9043845   0.36603285  0.55746074]]

print("sum =",np.sum(a,axis=1))
# sum = [ 1.96877324  2.43558896]

print("min =",np.min(a,axis=0))
# min = [ 0.23651224  0.41900661  0.36603285  0.46456022]

print("max =",np.max(a,axis=1))
# max = [ 0.84869417  0.9043845 ]

### 运算 & 索引
A = np.arange(2,14).reshape((3,4)) 

# array([[ 2, 3, 4, 5]
#        [ 6, 7, 8, 9]
#        [10,11,12,13]])
         
print(np.argmin(A))    # 0
print(np.argmax(A))    # 11

print(np.mean(A))        # 7.5
print(np.average(A))     # 7.5

print(A.mean())          # 7.5
print(np.median())       # 7.5 中位数
print(np.cumsum(A)) 
# [2 5 9 14 20 27 35 44 54 65 77 90] 累加
print(np.diff(A))    # 累差，最有一维元素个数会下降一个,[3,4] => [3,3]
# [[1 1 1]
#  [1 1 1]
#  [1 1 1]]

print(np.nonzero(A)) # 获取不为0的元素索引   
# (array([0,0,0,0,1,1,1,1,2,2,2,2]),array([0,1,2,3,0,1,2,3,0,1,2,3]))

A = np.arange(14,2, -1).reshape((3,4)) 

# array([[14, 13, 12, 11],
#       [10,  9,  8,  7],
#       [ 6,  5,  4,  3]])

print(np.sort(A))    

# array([[11,12,13,14]
#        [ 7, 8, 9,10]
#        [ 3, 4, 5, 6]])

print(np.transpose(A))    
print(A.T)

# array([[14,10, 6]
#        [13, 9, 5]
#        [12, 8, 4]
#        [11, 7, 3]])
# array([[14,10, 6]
#        [13, 9, 5]
#        [12, 8, 4]
#        [11, 7, 3]])

print(A)
# array([[14,13,12,11]
#        [10, 9, 8, 7]
#        [ 6, 5, 4, 3]])

print(np.clip(A,5,9))    
# array([[ 9, 9, 9, 9]
#        [ 9, 9, 8, 7]
#        [ 6, 5, 5, 5]])

#### 索引 & 切片

A = np.arange(3,15)

# array([3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14])
         
print(A[3])    # 6

A = np.arange(3,15).reshape((3,4))
"""
array([[ 3,  4,  5,  6]
       [ 7,  8,  9, 10]
       [11, 12, 13, 14]])
"""
         
print(A[2])         
# [11 12 13 14]

print(A[1][1])      # 8
print(A[1, 1])      # 8

print(A[1, 1:3])    # [8 9] 比较特殊

for row in A:
    print(row)
"""    
[ 3,  4,  5, 6]
[ 7,  8,  9, 10]
[11, 12, 13, 14]
"""

for column in A.T:
    print(column)
"""  
[ 3,  7,  11]
[ 4,  8,  12]
[ 5,  9,  13]
[ 6, 10,  14]
"""

import numpy as np
A = np.arange(3,15).reshape((3,4))
         
print(A.flatten())   
# array([3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14])

for item in A.flat:
    print(item)
    
# 3
# 4
# ……
# 14

#### 数组合并 
A = np.array([1,1,1])
B = np.array([2,2,2])
         
print(np.vstack((A,B)))    # vertical stack
"""
[[1,1,1]
 [2,2,2]]
"""
C = np.vstack((A,B))      
print(A.shape(),C.shape())    

# (3,) (2,3)

D = np.hstack((A,B))       # horizontal stack

print(D)
# [1,1,1,2,2,2]

print(A.shape(),D.shape())    
# (3,) (6,)

print(A[np.newaxis,:])
# [[1 1 1]]

print(A[np.newaxis,:].shape)
# (1,3)

print(A[:,np.newaxis])
"""
[[1]
[1]
[1]]
"""

print(A[:,np.newaxis].shape)
# (3,1)

A = np.array([1,1,1])[:,np.newaxis]
B = np.array([2,2,2])[:,np.newaxis]
         
C = np.vstack((A,B))   # vertical stack
D = np.hstack((A,B))   # horizontal stack

print(D)
"""
[[1 2]
[1 2]
[1 2]]
"""

print(A.shape,D.shape)
# (3,1) (3,2)

C = np.concatenate((A,B,B,A),axis=0)

print(C)
"""
array([[1],
       [1],
       [1],
       [2],
       [2],
       [2],
       [2],
       [2],
       [2],
       [1],
       [1],
       [1]])
"""

D = np.concatenate((A,B,B,A),axis=1)

print(D)
"""
array([[1, 2, 2, 1],
       [1, 2, 2, 1],
       [1, 2, 2, 1]])
"""

###### 分割
A = np.arange(12).reshape((3, 4))
print(A)
"""
array([[ 0,  1,  2,  3],
    [ 4,  5,  6,  7],
    [ 8,  9, 10, 11]])
"""
print(np.split(A, 2, axis=1))
"""
[array([[0, 1],
        [4, 5],
        [8, 9]]), array([[ 2,  3],
        [ 6,  7],
        [10, 11]])]
"""
print(np.split(A, 3, axis=0))

# [array([[0, 1, 2, 3]]), array([[4, 5, 6, 7]]), array([[ 8,  9, 10, 11]])]

print(np.split(A, 3, axis=1))

# ValueError: array split does not result in an equal division

print(np.array_split(A, 3, axis=1))
"""
[array([[0, 1],
        [4, 5],
        [8, 9]]), array([[ 2],
        [ 6],
        [10]]), array([[ 3],
        [ 7],
        [11]])]
"""

print(np.vsplit(A, 3)) #等于 print(np.split(A, 3, axis=0))

# [array([[0, 1, 2, 3]]), array([[4, 5, 6, 7]]), array([[ 8,  9, 10, 11]])]


print(np.hsplit(A, 2)) #等于 print(np.split(A, 2, axis=1))
"""
[array([[0, 1],
       [4, 5],
       [8, 9]]), array([[ 2,  3],
        [ 6,  7],
        [10, 11]])]
"""

##### 复制
a = np.arange(4)
# array([0, 1, 2, 3])
b = a
c = a
d = b
b is a  # True
c is a  # True
d is a  # True
d[1:3] = [22, 33]   # array([11, 22, 33,  3])
print(a)            # array([11, 22, 33,  3])
print(b)            # array([11, 22, 33,  3])
print(c)            # array([11, 22, 33,  3])

b = a.copy()    # deep copy
print(b)        # array([11, 22, 33,  3])
a[3] = 44
print(a)        # array([11, 22, 33, 44])
print(b)        # array([11, 22, 33,  3])