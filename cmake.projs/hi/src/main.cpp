#include "a.pb.h"
#include <google/protobuf/util/json_util.h>

#include <iostream>

using namespace std;
using namespace google::protobuf::util;

int main() {
    Xpb::Test t;
    t.mutable_b()->set_haha(12);
    t.set_name("hello");

    string json_string;
    MessageToJsonString(t, &json_string);

    cout << json_string << endl;
    return 0;
}