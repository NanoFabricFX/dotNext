using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DotNext.Net.Cluster.Consensus.Raft.Commands
{
    using Runtime.Serialization;
    using static Runtime.Intrinsics;

    public partial class CommandInterpreter
    {
        /// <summary>
        /// Represents builder of the interpreter.
        /// </summary>
        public sealed class Builder : ISupplier<CommandInterpreter>
        {
            private readonly Dictionary<int, CommandHandler> interpreters;
            private readonly Dictionary<Type, FormatterInfo> formatters;
            private int? snapshotCommandId;

            /// <summary>
            /// Initializes a new builder.
            /// </summary>
            public Builder()
            {
                interpreters = new();
                formatters = new();
            }

            /// <summary>
            /// Registers command handler.
            /// </summary>
            /// <remarks>
            /// <see cref="SerializableAttribute.Formatter"/> is ignored by this method.
            /// </remarks>
            /// <param name="handler">The command handler.</param>
            /// <param name="formatter">Serializer/deserializer of the command type.</param>
            /// <param name="snapshotHandler">
            /// <see langword="true"/> to register a handler for snapshot log entry;
            /// <see langword="false"/> to register a handler for regular log entry.
            /// </param>
            /// <typeparam name="TCommand">The type of the command supported by the handler.</typeparam>
            /// <returns>This builder.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="handler"/> or <paramref name="formatter"/> is <see langword="null"/>.</exception>
            /// <exception cref="GenericArgumentException">Type <typaparamref name="TCommand"/> is not annotated with <see cref="CommandAttribute"/> attribute.</exception>
            public Builder Add<TCommand>(Func<TCommand, CancellationToken, ValueTask> handler, IFormatter<TCommand> formatter, bool snapshotHandler = false)
                where TCommand : struct
            {
                if (handler is null)
                    throw new ArgumentNullException(nameof(handler));
                if (formatter is null)
                    throw new ArgumentNullException(nameof(formatter));

                var id = typeof(TCommand).GetCustomAttribute<CommandAttribute>()?.Id ?? throw new GenericArgumentException<TCommand>(ExceptionMessages.MissingCommandAttribute<TCommand>());
                interpreters.Add(id, new CommandHandler<TCommand>(formatter, handler));
                formatters.Add(typeof(TCommand), FormatterInfo.Create(formatter, id));
                if (snapshotHandler)
                    snapshotCommandId = id;
                return this;
            }

            /// <summary>
            /// Registers command handler.
            /// </summary>
            /// <remarks>
            /// <see cref="SerializableAttribute.Formatter"/> must be defined.
            /// </remarks>
            /// <param name="handler">The command handler.</param>
            /// <param name="snapshotHandler">
            /// <see langword="true"/> to register a handler for snapshot log entry;
            /// <see langword="false"/> to register a handler for regular log entry.
            /// </param>
            /// <typeparam name="TCommand">The type of the command supported by the handler.</typeparam>
            /// <returns>This builder.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
            /// <exception cref="GenericArgumentException">Type <typaparamref name="TCommand"/> is not annotated with <see cref="CommandAttribute"/> attribute or <see cref="SerializableAttribute.Formatter"/> refers to the invalid formatter.</exception>
            public Builder Add<TCommand>(Func<TCommand, CancellationToken, ValueTask> handler, bool snapshotHandler = false)
                where TCommand : struct
            {
                if (handler is null)
                    throw new ArgumentNullException(nameof(handler));

                var attr = typeof(TCommand).GetCustomAttribute<CommandAttribute>();
                if (attr is null || attr.Formatter is null)
                    throw new GenericArgumentException<TCommand>(ExceptionMessages.MissingCommandAttribute<TCommand>());

                var formatter = new FormatterInfo(attr);
                if (formatter.IsEmpty)
                    throw new GenericArgumentException<TCommand>(ExceptionMessages.MissingCommandFormatter<TCommand>());
                formatters.Add(typeof(TCommand), formatter);
                var interp = Activator.CreateInstance(typeof(CommandHandler<>).MakeGenericType(typeof(TCommand)), formatter, handler);
                interpreters.Add(attr.Id, Cast<CommandHandler>(interp));
                if (snapshotHandler)
                    snapshotCommandId = attr.Id;
                return this;
            }

            /// <summary>
            /// Clears this builder so it can be reused.
            /// </summary>
            public void Clear()
            {
                interpreters.Clear();
                formatters.Clear();
            }

            /// <summary>
            /// Constructs an instance of <see cref="CommandInterpreter"/>.
            /// </summary>
            /// <returns>A new instance of the interpreter.</returns>
            public CommandInterpreter Build() => new(interpreters, formatters, snapshotCommandId);

            /// <inheritdoc />
            CommandInterpreter ISupplier<CommandInterpreter>.Invoke() => Build();
        }
    }
}