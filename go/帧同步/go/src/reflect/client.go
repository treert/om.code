package main

import (
    "os"
    "fmt"
    "net"
    "reflect"
//  "io"
)

func checkError(err error){
    if  err != nil {
        fmt.Println("Error: %s", err.Error())
        os.Exit(1)
    }
}


func listen(conn net.Conn){
    var buf [1400]byte

    for{

        n, err := conn.Read(buf[0:])
        if err != nil {
            return
        }

        fmt.Println(" recv msg", string(buf[:n]),"end\n")
    }

}


func main() {
    conn, err := net.Dial("udp", "127.0.0.1:9999")
    checkError(err)
    fmt.Println("%T",reflect.TypeOf(conn))
    defer conn.Close()

    udp_conn,ok:=conn.(net.Conn)
    if !ok {
        return
    }
    go listen(udp_conn)

    for {
        var input_msg string
        fmt.Print("send: ")
        fmt.Scanln(&input_msg)
        _, err = conn.Write([]byte(input_msg))
        checkError(err)
    }
}