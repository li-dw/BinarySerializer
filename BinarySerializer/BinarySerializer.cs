﻿using System;
using System.Collections.Generic;
using System.IO;

namespace BinarySerialization
{
    /// <summary>
    /// Declaratively serializes and deserializes an object, or a graph of connected objects, in binary format.
    /// <seealso cref="IgnoreAttribute"/>
    /// <seealso cref="SerializeAsAttribute"/>
    /// <seealso cref="SerializeAsEnumAttribute"/>
    /// <seealso cref="FieldOffsetAttribute"/>
    /// <seealso cref="FieldLengthAttribute"/>
    /// <seealso cref="FieldCountAttribute"/>
    /// <seealso cref="SubtypeAttribute"/>
    /// <seealso cref="SerializeWhenAttribute"/>
    /// <seealso cref="SerializeUntilAttribute"/>
    /// <seealso cref="ItemLengthAttribute"/>
    /// <seealso cref="ItemSerializeUntilAttribute"/>
    /// <seealso cref="IBinarySerializable"/>
    /// </summary>
    public class BinarySerializer
    {
        private readonly Dictionary<Type, Node> _graphCache = new Dictionary<Type, Node>();
 
        /// <summary>
        /// The default <see cref="Endianness"/> to use during serialization or deserialization.
        /// This property can be updated dynamically during serialization or deserialization.
        /// </summary>
        public Endianness Endianness { get; set; }

        /// <summary>
        /// Occurrs after a member has been serialized.
        /// </summary>
        public event EventHandler<MemberSerializedEventArgs> MemberSerialized;

        /// <summary>
        /// Occurrs after a member has been deserialized.
        /// </summary>
        public event EventHandler<MemberSerializedEventArgs> MemberDeserialized;

        /// <summary>
        /// Occurrs before a member has been serialized.
        /// </summary>
        public event EventHandler<MemberSerializingEventArgs> MemberSerializing;

        /// <summary>
        /// Occurrs before a member has been deserialized.
        /// </summary>
        public event EventHandler<MemberSerializingEventArgs> MemberDeserializing;


        private Node GetGraph(Type valueType)
        {
            Node graph;
            if (!_graphCache.TryGetValue(valueType, out graph))
            {
                graph = new RootNode(valueType);
                graph.Bind();
                graph.MemberSerializing += OnMemberSerializing;
                graph.MemberSerialized += OnMemberSerialized;
                graph.MemberSerializing += OnMemberDeserializing;
                graph.MemberSerialized += OnMemberDeserialized;
                _graphCache.Add(valueType, graph);
            }

            return graph;
        }
        /// <summary>
        /// Serializes the object, or graph of objects with the specified top (root), to the given stream.
        /// </summary>
        /// <param name="stream">The stream to which the graph is to be serialized.</param>
        /// <param name="value">The object at the root of the graph to serialize.</param>
        /// <param name="context">An optional serialization context.</param>
        public void Serialize(Stream stream, object value, BinarySerializationContext context = null)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (value == null)
                return;

            Node graph = GetGraph(value.GetType());

            graph.Value = value;

            graph.Serialize(stream);
        }

        /// <summary>
        /// Calculates the serialized length of the object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <returns>The length of the specified object graph when serialized.</returns>
        public long SizeOf(object o)
        {
            var nullStream = new NullStream();
            Serialize(nullStream, o);
            return nullStream.Length;
        }
        
        /// <summary>
        /// Deserializes the specified stream into an object graph.
        /// </summary>
        /// <typeparam name="T">The type of the root of the object graph.</typeparam>
        /// <param name="stream">The stream from which to deserialize the object graph.</param>
        /// <param name="context">An optional serialization context.</param>
        /// <param name="formatProvider">An optional formatter.</param>
        /// <returns>The deserialized object graph.</returns>
        public T Deserialize<T>(Stream stream, BinarySerializationContext context = null,
                                IFormatProvider formatProvider = null) 
        {
            Node graph = GetGraph(typeof(T));

            graph.Deserialize(new StreamLimiter(stream));

            return (T)graph.Value;
        }

        /// <summary>
        /// Deserializes the specified stream into an object graph.
        /// </summary>
        /// <typeparam name="T">The type of the root of the object graph.</typeparam>
        /// <param name="data">The byte array from which to deserialize the object graph.</param>
        /// <param name="context">An optional serialization context.</param>
        /// <returns>The deserialized object graph.</returns>
        public T Deserialize<T>(byte[] data, BinarySerializationContext context = null) 
        {
            return Deserialize<T>(new MemoryStream(data), context);
        }


        private void OnMemberSerialized(object sender, MemberSerializedEventArgs e)
        {
            var handler = MemberSerialized;
            if (handler != null)
                handler(sender, e);
        }

        private void OnMemberDeserialized(object sender, MemberSerializedEventArgs e)
        {
            var handler = MemberDeserialized;
            if (handler != null)
                handler(sender, e);
        }

        private void OnMemberSerializing(object sender, MemberSerializingEventArgs e)
        {
            var handler = MemberSerializing;
            if (handler != null)
                handler(sender, e);
        }

        private void OnMemberDeserializing(object sender, MemberSerializingEventArgs e)
        {
            var handler = MemberDeserializing;
            if (handler != null)
                handler(sender, e);
        }
    }
}