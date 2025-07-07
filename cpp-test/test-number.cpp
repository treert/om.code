#include<iostream>
#include <iomanip>

using namespace std;

int main()
{
    /** 16_777_216 是float表示的连续整数区间的最大值。之后的码点间隔就大于1了。 */
    float f1 = (1<<24);// 16_777_216
    cout << "float: " << std::fixed << std::setprecision(3)
        << f1 << " "
        << (f1 == (f1 - 1)) << " "          // 0
        << (f1 == (f1 - 0.5f)) << " "       // 1
        << (f1 == (f1 + 0.5f)) << " "       // 1
        << (f1 == (f1 + 1)) << " "          // 1
        << (f1 == (f1 + 2)) << " "          // 0
        << endl;
    return 0;
}