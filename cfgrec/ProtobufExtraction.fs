﻿namespace cfgrecon
  module ProtobufExtraction =

    // architecture_t
    type Architecture =
      X86 = 0 | X86_64 = 1

    // address_t
    type Address () =
      inherit Froto.Core.Encoding.MessageBase()

      (* begin of primary constructor *)
      let m_value = ref (Value_32 Unchecked.defaultof<int32>)
      // let m_value = ref None

      let decode_v32_callback =
        fun raw_field ->
          let ref_v32 = ref (int32 0)
          Froto.Core.Encoding.Serializer.hydrateSInt32 ref_v32 raw_field;
          m_value := Value_32 (!ref_v32)

      let decode_v64_callback =
        fun raw_field ->
          let ref_v64 = ref (int64 0)
          Froto.Core.Encoding.Serializer.hydrateSInt64 ref_v64 raw_field;
          m_value := Value_64 (!ref_v64)

      let m_decoder_ring = Map.ofList [ 1, decode_v32_callback
                                        2, decode_v64_callback ]
      (* end of primary constructor *)

      member x.Value with get() = !m_value and set(v) = m_value := v

      override x.Clear () =
        m_value := (Value_32 Unchecked.defaultof<int32>)

      override x.Encode zc_buffer =
        match !m_value with
          | Value_32 v -> (v |> Froto.Core.Encoding.Serializer.dehydrateSInt32 1) zc_buffer
          | Value_64 v -> (v |> Froto.Core.Encoding.Serializer.dehydrateSInt64 2) zc_buffer
          // | _ -> failwith "invalid value"

      override x.DecoderRing = m_decoder_ring

      static member FromArraySegment (buffer : System.ArraySegment<byte>) =
        let self = Address()
        self.Merge(buffer) |> ignore
        self

    and UnionInt =
      | Value_32 of int32
      | Value_64 of int64
      // | None

    // register_t
    type Register () =
      inherit Froto.Core.Encoding.MessageBase()

      (* begin of primary constructor *)
      let m_name = ref Unchecked.defaultof<string>
      let m_value = ref (Address())

      let m_decoder_ring =
        Map.ofList [ 1, m_name |> Froto.Core.Encoding.Serializer.hydrateString
                     2, m_value |> Froto.Core.Encoding.Serializer.hydrateMessage (Address.FromArraySegment) ]
      (* end of primary constructor *)

      member x.Name with get() = !m_name and set(v) = m_name := v
      member x.Value with get () = !m_value and set(v) = m_value := v

      override x.Clear () =
        m_name := Unchecked.defaultof<string>;
        m_value := Address()

      override x.Encode zc_buffer =
        let encode =
          (!m_name |> Froto.Core.Encoding.Serializer.dehydrateString 1) >>
          (!m_value |> Froto.Core.Encoding.Serializer.dehydrateMessage 2)
        encode zc_buffer

      override x.DecoderRing = m_decoder_ring

      static member FromArraySegment (buffer : System.ArraySegment<byte>) =
        let self = Register()
        self.Merge(buffer) |> ignore
        self

    // memory_t
    type Memory () =
      inherit Froto.Core.Encoding.MessageBase()

      (* begin of primary constructor *)
      let m_address = ref (Address())
      let m_value = ref Unchecked.defaultof<int32>

      let m_decoder_ring =
        Map.ofList [ 1, m_address |> Froto.Core.Encoding.Serializer.hydrateMessage (Address.FromArraySegment)
                     2, m_value |> Froto.Core.Encoding.Serializer.hydrateSInt32 ]
      (* end of primary constructor *)

      member x.Address with get() = !m_address and set(v) = m_address := v
      member x.Value with get() = !m_value and set(v) = m_value := v

      override x.Clear () =
        m_address := Address();
        m_value := Unchecked.defaultof<int32>

      override x.Encode zc_buffer =
        let encode =
          (!m_address |> Froto.Core.Encoding.Serializer.dehydrateMessage 1) >>
          (!m_value |> Froto.Core.Encoding.Serializer.dehydrateSInt32 2)
        encode zc_buffer

      override x.DecoderRing = m_decoder_ring

      static member FromArraySegment (buffer : System.ArraySegment<byte>) =
        let self = Memory()
        ignore <| self.Merge(buffer)
        self

    // concrete_info_t
    type ConcreteInfo () =
      inherit Froto.Core.Encoding.MessageBase()

      (* begin of primary constructor *)
      // let m_value = ref None
      let m_value = ref <| ReadRegister (Register())

      // false => read, true => write
      let decode_register_callback read_or_write =
        fun raw_field ->
          let ref_register = ref (Register())
          Froto.Core.Encoding.Serializer.hydrateMessage (Register.FromArraySegment) ref_register raw_field;
          if read_or_write then
            m_value := ReadRegister (!ref_register)
          else
            m_value := WriteRegister (!ref_register)

      // false => load, true => store
      let decode_memory_callback load_or_store =
        fun raw_field ->
          let ref_memory = ref (Memory())
          Froto.Core.Encoding.Serializer.hydrateMessage (Memory.FromArraySegment) ref_memory raw_field;
          if load_or_store then
            m_value := LoadMemory (!ref_memory)
          else
            m_value := StoreMemory (!ref_memory)

      let m_decoder_ring =
        Map.ofList [ 1, decode_register_callback false
                     2, decode_register_callback true
                     3, decode_memory_callback false
                     4, decode_memory_callback true ]
      (* end of primary constructor *)

      member x.Value with get() = !m_value and set(v) = m_value := v

      override x.Clear () =
        m_value := ReadRegister (Register())

      override x.Encode zc_buffer =
        match !m_value with
          | ReadRegister v -> (v |> Froto.Core.Encoding.Serializer.dehydrateMessage 1) zc_buffer
          | WriteRegister v -> (v |> Froto.Core.Encoding.Serializer.dehydrateMessage 2) zc_buffer
          | LoadMemory v -> (v |> Froto.Core.Encoding.Serializer.dehydrateMessage 3) zc_buffer
          | StoreMemory v -> (v |> Froto.Core.Encoding.Serializer.dehydrateMessage 4) zc_buffer
          | _ -> failwith "invalid value"

      override x.DecoderRing = m_decoder_ring

      static member FromArraySegment (buffer : System.ArraySegment<byte>) =
        let self = ConcreteInfo()
        ignore <| self.Merge(buffer)
        self

    and UnionInfo =
      | ReadRegister of Register
      | WriteRegister of Register
      | LoadMemory of Memory
      | StoreMemory of Memory
      // | None

    // instruction_t
    type Instruction () =
      inherit Froto.Core.Encoding.MessageBase()

      (* begin of primary constructor *)
      let m_thread_id = ref Unchecked.defaultof<int32>
      let m_address = ref (Address())
      let m_opcode = ref Unchecked.defaultof<byte[]>
      let m_disassemble = ref Unchecked.defaultof<string>
      let m_c_info = ref List.empty<ConcreteInfo>

      let m_decoder_ring =
        Map.ofList [ 1, m_thread_id |> Froto.Core.Encoding.Serializer.hydrateSInt32
                     2, m_address |> Froto.Core.Encoding.Serializer.hydrateMessage (Address.FromArraySegment)
                     3, m_opcode |> Froto.Core.Encoding.Serializer.hydrateBytes
                     4, m_disassemble |> Froto.Core.Encoding.Serializer.hydrateString
                     5, m_c_info |> Froto.Core.Encoding.Serializer.hydrateRepeated (Froto.Core.Encoding.Serializer.hydrateMessage (ConcreteInfo.FromArraySegment)) ]

      // let c_info_encode_callback field_num (c_info:ConcreteInfo) =
      //   Froto.Core.Encoding.Serializer.dehydrateMessage field_num c_info

      (* end of primary constructor *)

      member x.ThreadId with get() = !m_thread_id and set(v) = m_thread_id := v
      member x.Address with get() = !m_address and set(v) = m_address := v
      member x.Opcode with get() = !m_opcode and set(v) = m_opcode := v
      member x.Disassemble with get() = !m_disassemble and set(v) = m_disassemble := v
      member x.ConcreteInfo with get() = !m_c_info and set(v) = m_c_info := v

      override x.Clear () =
        m_thread_id := Unchecked.defaultof<int32>;
        m_address := Address();
        m_opcode := Unchecked.defaultof<byte[]>;
        m_disassemble := Unchecked.defaultof<string>;
        m_c_info := List.empty<ConcreteInfo>

      override x.Encode zc_buffer =
        let encode =
          (Froto.Core.Encoding.Serializer.dehydrateSInt32 1 !m_thread_id) >>
          (Froto.Core.Encoding.Serializer.dehydrateMessage 2 !m_address) >>
          (Froto.Core.Encoding.Serializer.dehydrateBytes 3 <| System.ArraySegment(!m_opcode)) >>
          (Froto.Core.Encoding.Serializer.dehydrateString 4 !m_disassemble) >>
          (Froto.Core.Encoding.Serializer.dehydrateRepeated Froto.Core.Encoding.Serializer.dehydrateMessage 5 !m_c_info)
        encode zc_buffer

      override x.DecoderRing = m_decoder_ring

      static member FromArraySegment (buffer:System.ArraySegment<byte>) =
        let self = Instruction()
        ignore <| self.Merge(buffer)
        self

    // header_t
    type Header () =
      inherit Froto.Core.Encoding.MessageBase()

      (* begin of primary constructor *)
      let m_arch = ref Architecture.X86

      let m_decoder_ring = Map.ofList [ 1, m_arch |> Froto.Core.Encoding.Serializer.hydrateEnum ]
      (* end of primary constructor *)

      member x.Architecture with get() = !m_arch and set(v) = m_arch := v

      override x.Clear () = m_arch := Architecture.X86

      override x.Encode zc_buffer =
        Froto.Core.Encoding.Serializer.dehydrateDefaultedVarint Architecture.X86 1 !m_arch zc_buffer

      override x.DecoderRing = m_decoder_ring

      static member FromArraySegment (buffer:System.ArraySegment<byte>) =
        let self = Header()
        ignore <| self.Merge(buffer)
        self

    type Chunk () =
      inherit Froto.Core.Encoding.MessageBase()

      (* begin of primary constructor *)
      let m_insts = ref List.empty<Instruction>

      let m_decoder_ring =
        Map.ofList [ 1, m_insts |> Froto.Core.Encoding.Serializer.hydrateRepeated
                                   (Froto.Core.Encoding.Serializer.hydrateMessage Instruction.FromArraySegment) ]
      (* end of primary constructor *)

      member x.Instructions with get() = !m_insts and set(v) = m_insts := v

      override x.Clear () = m_insts := List.empty<Instruction>

      override x.Encode zc_buffer =
        Froto.Core.Encoding.Serializer.dehydrateRepeated Froto.Core.Encoding.Serializer.dehydrateMessage 1 !m_insts zc_buffer

      override x.DecoderRing = m_decoder_ring

      static member FromArraySegment (buffer:System.ArraySegment<byte>) =
        let self = Chunk()
        ignore <| self.Merge(buffer)
        self

    let private read_data_block (reader:System.IO.BinaryReader) =
      reader.ReadUInt32() |> int |> reader.ReadBytes

    let extract_machine_info (reader:System.IO.BinaryReader) =
      try
        let header_block = read_data_block reader
        let header_segment = System.ArraySegment(header_block)
        let header = Header()
        ignore (header.DeserializeLengthDelimited <| Froto.Core.ZeroCopyBuffer(header_segment))
        match header.Architecture with
          | Architecture.X86 -> Some Machine.X86
          | Architecture.X86_64 -> Some Machine.X86_64
          | _ -> None
      with
        | _ -> None

    let convert_to_explicit_address<'T> (addr:Address) =
      match addr.Value with
        | Value_64 v -> int64 v |> unbox<'T>
        | Value_32 v -> int32 v |> unbox<'T>

    let convert_to_explicit_instruction<'T when 'T : comparison> (ins:Instruction) =
      let explicit_thread_id = ins.ThreadId
      let explicit_address = convert_to_explicit_address ins.Address
      let explicit_opcode = ins.Opcode
      let explicit_disassemble = ins.Disassemble
      let conc_info_list = ins.ConcreteInfo
      let mem_load_map = ref Map.empty
      let mem_store_map = ref Map.empty
      let reg_read_map = ref Map.empty
      let reg_write_map = ref Map.empty
      for conc_info in conc_info_list do
        match conc_info.Value with
          | ReadRegister read_reg -> reg_read_map := Map.ofList ((read_reg.Name, convert_to_explicit_address<'T>(read_reg.Value))::(Map.toList !reg_read_map))



    let extract_instructions (reader:System.IO.BinaryReader) =
      let extracted_inss = ref Seq.empty
      let should_continue_parsing = ref true
      while !should_continue_parsing do
        try
          let chunk_block = read_data_block reader
          let chunk_segment = System.ArraySegment(chunk_block)
          let chunk = Chunk()
          ignore (chunk.DeserializeLengthDelimited <| Froto.Core.ZeroCopyBuffer(chunk_segment))
          let chunk_inss = chunk.Instructions
          extracted_inss := Seq.ofList chunk_inss |> Seq.append !extracted_inss
        with
          | :? System.IO.EndOfStreamException -> should_continue_parsing := false
      
