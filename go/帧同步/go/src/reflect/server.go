package main

import (
    "os"
    "fmt"
    "net"
    "time"
    "encoding/binary"
)

const (
    MTU = 258
    FAME_PER_MS = 17
    LONG_LIMIT = 100*60*1000
)

func checkError(err error){
    if  err != nil {
        fmt.Println("Error: %s", err.Error())
        os.Exit(1)
    }
}

var _cur_timestamp int64

func generate_timestamp() {
    _cur_timestamp = time.Now().UnixNano() / int64(time.Millisecond)
}

func get_timestamp() int64{
    return _cur_timestamp
}

func main() {
    connect_manager.connect_manager_ctor()
    frame_manager.frame_manager_ctor()
    for {
        connect_manager.recv_one_pack()
    }
}

// 连接管理
type ConnectManager struct{
    listen_socket *net.UDPConn
    cur_client_addr *net.UDPAddr
}

var connect_manager = ConnectManager{}

func (this*ConnectManager) connect_manager_ctor(){

    udp_addr, err := net.ResolveUDPAddr("udp", ":9999")
    checkError(err)

    conn, err := net.ListenUDP("udp", udp_addr)
    checkError(err)

    this.listen_socket = conn
}

func (this*ConnectManager) recv_one_pack(){
    var buf [MTU]byte
    n,raddr,err := this.listen_socket.ReadFromUDP(buf[0:])
    if err != nil {
        return
    }
    this.cur_client_addr = raddr
    generate_timestamp()
    frame_manager.handle_udp_pack(buf[0:n])
}

func (this*ConnectManager) send_one_pack(data[]byte){
    _, err := this.listen_socket.WriteToUDP(data,this.cur_client_addr)
    checkError(err)
}

// 帧管理
type FrameManager struct{
    battle_map map[int32]*OneBattle
}

var frame_manager = FrameManager{}

func (this*FrameManager) frame_manager_ctor() {
    this.battle_map = make(map[int32]*OneBattle)
}

func (this*FrameManager) handle_udp_pack(pack []byte){
    var pack_len = len(pack)
    if (pack_len < 20) || (pack_len%4 != 0){
        return
    }

    var sid,idx,uin,length int32
    sid = int32(binary.BigEndian.Uint32(pack[0:]))
    idx = int32(binary.BigEndian.Uint32(pack[4:]))
    uin = int32(binary.BigEndian.Uint32(pack[8:]))
    //opt = int32(binary.BigEndian.Uint32(pack[12:]))
    length = int32(binary.BigEndian.Uint32(pack[16:]))
    var data = pack[20:]
    if length != int32(len(data)) {
        return
    }

    var battle,ok = this.battle_map[sid]
    if (!ok) || ((get_timestamp() - battle.start_time) > LONG_LIMIT){
        battle = &OneBattle{}
        battle.sid = sid
        battle.start_time = get_timestamp()
        battle.start_second = time.Now().Unix()
        battle.input_map = make(map[int32]*OneInput)
        this.battle_map[sid] = battle
        fmt.Println("add new battle sid",sid,idx,uin,"cur time",battle.start_second)
    }

    battle.handle_one_input(uin,data)

    var one_frame_data = battle.get_data(idx-1)
    connect_manager.send_one_pack(one_frame_data)
}

type OneBattle struct{
    sid int32
    start_time int64
    start_second int64
    cur_time int64
    frames [][]byte
    idx_offset_arr []int32
    frame_history []byte
    input_map map[int32]*OneInput
}

func (this*OneBattle) handle_one_input(uin int32,data []byte){
    this.cur_time = get_timestamp()
    // 合并输入
    var one_input,ok = this.input_map[uin]
    if !ok {
        one_input = &OneInput{}
        one_input.uin = uin
        this.input_map[uin] = one_input
    }
    one_input.one_input_add(data)

    // 更新帧历史
    var total_frame = (int32)((this.cur_time-this.start_time)/FAME_PER_MS)
    total_frame -= 60*3
    var idx = (int32)(len(this.frames))
    for ;idx < total_frame-1; idx++{
        // 空帧
        var frame = make([]byte,8)
        binary.BigEndian.PutUint32(frame[0:],uint32(8))
        binary.BigEndian.PutUint32(frame[4:],uint32(idx+1))
        this.frames = append(this.frames,frame)
    }
    if idx == total_frame - 1 {
        var frame = make([]byte,8)
        binary.BigEndian.PutUint32(frame[4:],uint32(idx+1))
        var frame_len = 8
        for _,input := range this.input_map {
            var one_data = input.one_input_get_and_clear()
            frame_len += len(one_data)
            frame = append(frame,one_data...)
        }
        binary.BigEndian.PutUint32(frame[0:],uint32(frame_len))
        this.frames = append(this.frames,frame)
        fmt.Println("add frame ",total_frame,"cur time", time.Now().Unix()-this.start_second)
    }
}

func (this*OneBattle) get_data(idx int32)[]byte {
    var ret = make([]byte,12)
    binary.BigEndian.PutUint32(ret[0:],uint32(this.sid))
    binary.BigEndian.PutUint32(ret[4:],uint32(0))
    binary.BigEndian.PutUint32(ret[8:],uint32(0))
    if idx < 0{
        fmt.Println("get_data error idx < 0, idx is",idx)
        return ret
    }

    var total_len = int32(12)
    var total_frame = int32(len(this.frames))
    for ;idx < total_frame ; idx ++ {
        var frame = this.frames[idx]
        var cur_len = int32(binary.BigEndian.Uint32(frame))
        total_len += cur_len
        ret = append(ret,frame...)
        if total_len > MTU {
            break
        }
    }
    total_len -= 12
    binary.BigEndian.PutUint32(ret[8:],uint32(total_len))
    return ret
}


type OneInput struct{
    uin int32
    data []byte
}

func (this*OneInput) one_input_add(data[]byte){
    this.data = append(this.data,data...)
}

func (this*OneInput) one_input_get_and_clear()[]byte{
    var ret []byte
    var data []byte
    var length = len(this.data)
    if length > 80 {
        length = 80
        data = this.data[length-80:]
    } else {
        data = this.data[0:]
    }

    ret = make([]byte,length+8)
    binary.BigEndian.PutUint32(ret[0:],uint32(length+8))
    binary.BigEndian.PutUint32(ret[4:],uint32(this.uin))
    copy(ret[8:],data)
    // append(ret[8:],data...)

    this.data = nil
    return ret
}