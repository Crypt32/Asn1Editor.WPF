using System.ComponentModel;
using System.Numerics;
using SysadminsLV.Asn1Editor.Core.Tree;
using SysadminsLV.Asn1Parser;
using SysadminsLV.Asn1Parser.Universal;

namespace Asn1Editor.Tests;

/// <summary>
/// Comprehensive test suite for TreeCoordinator covering node addition, insertion, removal, and updates
/// with various scenarios including ASN.1 length encoding boundary crossings.
/// </summary>
[TestClass]
public sealed class TreeCoordinatorTests {
    TreeCoordinator coordinator = null!;
    NodeViewOptionsMock viewOptions = null!;

    [TestInitialize]
    public void TestInitialize() {
        viewOptions = new NodeViewOptionsMock();
        coordinator = new TreeCoordinator(viewOptions);
    }

    #region Helper Methods

    /// <summary>
    /// Creates ASN.1 SEQUENCE with specified payload length.
    /// Format: SEQUENCE { children... }
    /// </summary>
    static Byte[] CreateSequence(params Byte[][] children) {
        Byte[] payload = children.SelectMany(c => c).ToArray();
        return Asn1Utils.Encode(payload, 0x30);
    }

    /// <summary>
    /// Creates ASN.1 INTEGER with specified value.
    /// </summary>
    static Byte[] CreateInteger(Byte value) {
        return Asn1Builder.Create().AddInteger(value).GetRawData();
    }

    /// <summary>
    /// Creates ASN.1 INTEGER with multi-byte value.
    /// </summary>
    static Byte[] CreateInteger(params Byte[] value) {
        BigInteger bigInteger = new BigInteger(value);
        return  Asn1Builder.Create().AddInteger(bigInteger).GetRawData();
    }

    /// <summary>
    /// Creates ASN.1 PrintableString with specified content.
    /// </summary>
    static Byte[] CreateString(String value) {
        return Asn1Builder.Create().AddPrintableString(value).GetRawData();
    }

    /// <summary>
    /// Creates ASN.1 NULL.
    /// </summary>
    static Byte[] CreateNull() {
        return new Asn1Null().GetRawData();
    }

    /// <summary>
    /// Creates a sequence with total payload length exactly at specified size (for boundary testing).
    /// </summary>
    static Byte[] CreateOctetStringWithPayloadLength(Int32 targetLength) {
        Byte[] lengthByes = Asn1Utils.GetLengthBytes(targetLength);
        targetLength = targetLength - lengthByes.Length - 1; // Adjust for length encoding bytes to skip tag and length bytes

        return new Asn1OctetString(new Byte[targetLength], false).GetRawData();
    }

    /// <summary>
    /// Asserts that a node's offset and all its descendants are correctly positioned.
    /// </summary>
    static void AssertNodeOffsets(AsnTreeNode node, Int32 expectedOffset) {
        Assert.AreEqual(expectedOffset, node.Value.Offset, $"Node at path {node.Path} has incorrect offset");
        
        Int32 childOffset = node.Value.Offset + node.Value.HeaderLength;
        foreach (AsnTreeNode child in node.Children) {
            AssertNodeOffsets(child, childOffset);
            childOffset += child.Value.TagLength;
        }
    }

    /// <summary>
    /// Asserts that sibling nodes have consecutive offsets.
    /// </summary>
    static void AssertSiblingOffsets(AsnTreeNode parent) {
        Int32 expectedOffset = parent.Value.Offset + parent.Value.HeaderLength;
        for (Int32 i = 0; i < parent.Children.Count; i++) {
            AsnTreeNode child = parent.Children[i];
            Assert.AreEqual(expectedOffset, child.Value.Offset, 
                $"Child[{i}] at path {child.Path} has incorrect offset");
            Assert.AreEqual(i, child.MyIndex, $"Child[{i}] has incorrect MyIndex");
            expectedOffset += child.Value.TagLength;
        }
    }

    /// <summary>
    /// Asserts that the binary data matches the tree structure.
    /// </summary>
    void AssertBinaryDataIntegrity() {
        if (coordinator.Root == null) {
            return;
        }

        // Total binary data should match root's TagLength
        Assert.HasCount(coordinator.Root.Value.TagLength, coordinator.RawData,
            "Binary data length does not match root TagLength");
        
        // Verify all nodes are within bounds
        AssertNodeWithinBounds(coordinator.Root);
    }

    void AssertNodeWithinBounds(AsnTreeNode node) {
        Assert.IsGreaterThanOrEqualTo(0, node.Value.Offset, $"Node {node.Path} has negative offset");
        Assert.IsLessThanOrEqualTo(coordinator.RawData.Count,
            node.Value.Offset + node.Value.TagLength, $"Node {node.Path} extends beyond binary data");
        
        foreach (AsnTreeNode child in node.Children) {
            AssertNodeWithinBounds(child);
        }
    }

    /// <summary>
    /// Gets the actual length encoding byte count for a given payload length.
    /// </summary>
    static Int32 GetLengthEncodingSize(Int32 payloadLength) {
        return Asn1Utils.GetLengthBytes(payloadLength).Length;
    }

    #endregion

    #region 1. Node Addition (AddNode) - No Length Encoding Growth

    [TestMethod]
    public async Task AddNode_AppendToRoot_FlatTree_NoLengthGrowth() {
        // Arrange
        Byte[] root = CreateSequence(CreateInteger(1), CreateInteger(2));
        await coordinator.InitializeFromRawData(root);
        Byte[] newChild = CreateInteger(3);
        
        Int32 initialRootLength = coordinator.Root!.Value.PayloadLength;
        Int32 initialChildCount = coordinator.Root.Children.Count;
        
        // Act
        AsnTreeNode addedNode = await coordinator.AddNode(newChild, coordinator.Root);
        
        // Assert
        Assert.HasCount(initialChildCount + 1, coordinator.Root.Children, "Child count should increase by 1");
        Assert.AreEqual(initialRootLength + newChild.Length, coordinator.Root.Value.PayloadLength,
            "Root payload length should increase by new child size");
        Assert.AreEqual(1, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength),
            "Root length encoding should remain 1 byte");
        Assert.AreEqual(newChild.Length, addedNode.Value.TagLength, "Added node should have correct size");
        AssertSiblingOffsets(coordinator.Root);
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task AddNode_AppendToRootWithSiblings_NoLengthGrowth() {
        // Arrange
        Byte[] child1 = CreateInteger(1);
        Byte[] child2 = CreateInteger(2);
        Byte[] root = CreateSequence(child1, child2);
        await coordinator.InitializeFromRawData(root);
        
        Byte[] newChild = CreateNull();
        Int32 child2InitialOffset = coordinator.Root!.Children[1].Value.Offset;
        
        // Act
        await coordinator.AddNode(newChild, coordinator.Root);
        
        // Assert
        Assert.HasCount(3, coordinator.Root.Children);
        Assert.AreEqual(child2InitialOffset, coordinator.Root.Children[1].Value.Offset,
            "Existing child offsets should not change when appending to end");
        AssertSiblingOffsets(coordinator.Root);
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task AddNode_AppendToDeepContainer_NoLengthGrowth() {
        // Arrange: Root -> Child -> Grandchild (container) -> GreatGrandchildren
        Byte[] greatGrandchild1 = CreateInteger(1);
        Byte[] greatGrandchild2 = CreateInteger(2);
        Byte[] grandchild = CreateSequence(greatGrandchild1, greatGrandchild2);
        Byte[] child = CreateSequence(grandchild);
        Byte[] root = CreateSequence(child);
        
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode targetContainer = coordinator.Root!.Children[0].Children[0]; // grandchild
        Byte[] newGreatGrandchild = CreateInteger(3);
        
        Int32 initialGrandchildLength = targetContainer.Value.PayloadLength;
        Int32 initialRootLength = coordinator.Root.Value.PayloadLength;
        
        // Act
        await coordinator.AddNode(newGreatGrandchild, targetContainer);
        
        // Assert
        Assert.HasCount(3, targetContainer.Children, "Grandchild should have 3 children");
        Assert.AreEqual(initialGrandchildLength + newGreatGrandchild.Length, 
            targetContainer.Value.PayloadLength,
            "Grandchild payload should increase");
        Assert.AreEqual(initialRootLength + newGreatGrandchild.Length,
            coordinator.Root.Value.PayloadLength,
            "Root payload should increase by same amount");
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task AddNode_AppendToContainerWithSiblingsAtMultipleLevels_NoLengthGrowth() {
        // Arrange: Root has 2 children, Child[0] has 2 grandchildren
        Byte[] grandchild1 = CreateInteger(1);
        Byte[] grandchild2 = CreateInteger(2);
        Byte[] child0 = CreateSequence(grandchild1, grandchild2);
        Byte[] child1 = CreateInteger(99);
        Byte[] root = CreateSequence(child0, child1);
        
        await coordinator.InitializeFromRawData(root);
        
        Int32 child1InitialOffset = coordinator.Root!.Children[1].Value.Offset;
        Byte[] newGrandchild = CreateInteger(3);
        
        // Act
        await coordinator.AddNode(newGrandchild, coordinator.Root.Children[0]);
        
        // Assert
        Assert.HasCount(3, coordinator.Root.Children[0].Children);
        Assert.AreEqual(child1InitialOffset + newGrandchild.Length, 
            coordinator.Root.Children[1].Value.Offset,
            "Child[1] offset should shift by size of new grandchild");
        AssertSiblingOffsets(coordinator.Root);
        AssertBinaryDataIntegrity();
    }

    #endregion

    #region 2. Node Addition (AddNode) - WITH Length Encoding Growth

    [TestMethod]
    public async Task AddNode_RootLengthGrowth_1To2Bytes_127To128() {
        // Arrange: Create root with payload length = 126 (will grow to 128)
        Byte[] child0 = CreateOctetStringWithPayloadLength(126);
        Byte[] root = CreateSequence(child0);
        await coordinator.InitializeFromRawData(root);
        
        Byte[] newChild = CreateNull(); // 2 bytes
        Int32 child0InitialOffset = coordinator.Root!.Children[0].Value.Offset;
        
        // Act
        await coordinator.AddNode(newChild, coordinator.Root);
        
        // Assert
        Assert.AreEqual(128, coordinator.Root.Value.PayloadLength, "Payload should be 128");
        Assert.AreEqual(2, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength),
            "Length encoding should be 2 bytes");
        Assert.AreEqual(child0InitialOffset + 1, coordinator.Root.Children[0].Value.Offset,
            "First child offset should shift by +1 due to length encoding growth");
        
        // All children should have shifted by +1
        foreach (AsnTreeNode child in coordinator.Root.Children) {
            AssertNodeOffsets(child, child.Value.Offset);
        }
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task AddNode_MidLevelContainerLengthGrowth_1To2Bytes() {
        // Arrange: Root -> Child (PayloadLength=126) -> Grandchildren
        Byte[] grandchildren = CreateOctetStringWithPayloadLength(126);
        Byte[] child = CreateSequence(grandchildren);
        Byte[] sibling = CreateInteger(99);
        Byte[] root = CreateSequence(child, sibling);
        
        await coordinator.InitializeFromRawData(root);
        
        Int32 rootInitialPayloadLength = coordinator.Root!.Value.PayloadLength;
        Int32 siblingInitialOffset = coordinator.Root!.Children[1].Value.Offset;
        Int32 grandchildInitialOffset = coordinator.Root.Children[0].Children[0].Value.Offset;
        
        Byte[] newGrandchild = CreateNull(); // 2 bytes, will make child's payload = 128
        
        // Act
        await coordinator.AddNode(newGrandchild, coordinator.Root.Children[0]);
        
        // Assert
        Assert.AreEqual(128, coordinator.Root.Children[0].Value.PayloadLength);
        Assert.AreEqual(2, GetLengthEncodingSize(coordinator.Root.Children[0].Value.PayloadLength));
        
        // Child's descendants should shift by +1 due to its header growth
        Assert.AreEqual(grandchildInitialOffset + 1, 
            coordinator.Root.Children[0].Children[0].Value.Offset,
            "Grandchildren should shift by +1");
        
        // Sibling should shift by +3 (2 bytes new node + 1 byte header growth)
        Assert.AreEqual(siblingInitialOffset + 3,
            coordinator.Root.Children[1].Value.Offset,
            "Sibling should shift by +3");
        
        // Root payload should increase by 3
        Assert.AreEqual(rootInitialPayloadLength + 3, // child(131) + sibling(3)
            coordinator.Root.Value.PayloadLength);
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task AddNode_CascadingLengthGrowth_MultipleLevels() {
        // Arrange: Create a 3-level tree where all containers are near the 127-byte boundary
        // Root.payload = 126 (1 byte length encoding)
        //     Child.payload = 124 (1 byte length encoding, total = 126)
        //         Grandchild.Payload = 122 (1 byte length encoding, total = 124)
    
        // Build from bottom up:
        Byte[] grandchildContent = CreateOctetStringWithPayloadLength(124); // Grandchild payload = 122, total = 124
        Byte[] child = CreateSequence(grandchildContent); // Child payload = 124, total = 126
        Byte[] root = CreateSequence(child); // Root payload = 126, total = 128
    
        await coordinator.InitializeFromRawData(root);
    
        // Pre-flight assertions
        Assert.AreEqual(126, coordinator.Root!.Value.PayloadLength);
        Assert.AreEqual(1, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength), 
            "Root should use 1-byte length encoding");
        Assert.AreEqual(124, coordinator.Root.Children[0].Value.PayloadLength);
        Assert.AreEqual(1, GetLengthEncodingSize(coordinator.Root.Children[0].Value.PayloadLength),
            "Child should use 1-byte length encoding");
        Assert.AreEqual(122, coordinator.Root.Children[0].Children[0].Value.PayloadLength);
        Assert.AreEqual(1, GetLengthEncodingSize(coordinator.Root.Children[0].Children[0].Value.PayloadLength),
            "Grandchild should use 1-byte length encoding");
    
        AsnTreeNode grandchild = coordinator.Root.Children[0].Children[0];
        Byte[] newNode = CreateInteger(0x01, 0x02, 0x03, 0x04); // 6 bytes total (tag + len + 4-byte value)
    
        // Act
        await coordinator.AddNode(newNode, grandchild);
    
        // Assert - Verify cascading length growth
        // Grandchild: 122 + 6 = 128 bytes (crosses boundary, header grows by 1)
        Assert.AreEqual(128, grandchild.Value.PayloadLength, 
            "Grandchild payload should cross to 128");
        Assert.AreEqual(2, GetLengthEncodingSize(grandchild.Value.PayloadLength),
            "Grandchild should grow to 2-byte length encoding");
    
        // Child: 124 + 7 (6 new + 1 grandchild header growth) = 131 bytes (crosses boundary, header grows by 1)
        AsnTreeNode assertChild = coordinator.Root.Children[0];
        Assert.AreEqual(131, assertChild.Value.PayloadLength,
            "Child payload should be 131 (124 + 6 node + 1 grandchild header)");
        Assert.AreEqual(2, GetLengthEncodingSize(assertChild.Value.PayloadLength),
            "Child should grow to 2-byte length encoding");
    
        // Root: 126 + 8 (6 new + 1 grandchild + 1 child header growth) = 134 bytes (crosses boundary)
        Assert.AreEqual(134, coordinator.Root.Value.PayloadLength,
            "Root payload should be 134 (126 + 6 node + 1 grandchild header + 1 child header)");
        Assert.AreEqual(2, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength),
            "Root should grow to 2-byte length encoding");
    
        // Total root size: 1 (tag) + 2 (new len) + 134 (payload) = 137
        Assert.AreEqual(137, coordinator.Root.Value.TagLength,
            "Root total size should be 137");
    
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task AddNode_RootLengthGrowth_2To3Bytes_255To256() {
        // Arrange: Create root with payload length = 254
        Byte[] child0 = CreateOctetStringWithPayloadLength(254);
        Byte[] root = CreateSequence(child0);
        await coordinator.InitializeFromRawData(root);
        
        Assert.AreEqual(2, GetLengthEncodingSize(coordinator.Root!.Value.PayloadLength),
            "Initial length encoding should be 2 bytes");
        
        Byte[] newChild = CreateNull(); // 2 bytes -> total 256
        
        // Act
        await coordinator.AddNode(newChild, coordinator.Root);
        
        // Assert
        Assert.AreEqual(256, coordinator.Root.Value.PayloadLength);
        Assert.AreEqual(3, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength),
            "Length encoding should grow to 3 bytes");
        
        AssertBinaryDataIntegrity();
    }

    #endregion

    #region 3. Node Insertion (InsertNode) - No Length Encoding Growth

    [TestMethod]
    public async Task InsertNode_BeforeFirstChild_NoLengthGrowth() {
        // Arrange
        Byte[] child1 = CreateInteger(1);
        Byte[] child2 = CreateInteger(2);
        Byte[] root = CreateSequence(child1, child2);
        await coordinator.InitializeFromRawData(root);
        
        Byte[] newChild = CreateInteger(0);
        Int32 child1InitialOffset = coordinator.Root!.Children[0].Value.Offset;
        
        // Act
        await coordinator.InsertNode(coordinator.Root.Children[0], NodeAddOption.Before, newChild);
        
        // Assert
        Assert.HasCount(3, coordinator.Root.Children);
        Assert.AreEqual("0", coordinator.Root.Children[0].Value.ExplicitValue, "New child should be at index 0");
        Assert.AreEqual(child1InitialOffset, coordinator.Root.Children[0].Value.Offset,
            "New child should be at old child1 offset");
        Assert.AreEqual(child1InitialOffset + newChild.Length, coordinator.Root.Children[1].Value.Offset,
            "Old child1 should shift by new child size");
        
        AssertSiblingOffsets(coordinator.Root);
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task InsertNode_AfterMiddleChild_NoLengthGrowth() {
        // Arrange
        Byte[] root = CreateSequence(CreateInteger(1), CreateInteger(2), CreateInteger(3));
        await coordinator.InitializeFromRawData(root);
        
        Byte[] newChild = CreateNull();
        Int32 child2InitialOffset = coordinator.Root!.Children[2].Value.Offset;
        
        // Act
        await coordinator.InsertNode(coordinator.Root.Children[1], NodeAddOption.After, newChild);
        
        // Assert
        Assert.HasCount(4, coordinator.Root.Children);
        Assert.AreEqual(2, coordinator.Root.Children[2].MyIndex, "New child should be at index 2");
        Assert.AreEqual(child2InitialOffset + newChild.Length, coordinator.Root.Children[3].Value.Offset,
            "Old child[2] should shift");
        
        AssertSiblingOffsets(coordinator.Root);
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task InsertNode_AsLastChild_DeepTree_NoLengthGrowth() {
        // Arrange
        Byte[] grandchild1 = CreateInteger(1);
        Byte[] grandchild2 = CreateInteger(2);
        Byte[] child = CreateSequence(grandchild1, grandchild2);
        Byte[] root = CreateSequence(child);
        
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode target = coordinator.Root!.Children[0]; // child
        Byte[] newGrandchild = CreateInteger(3);
        
        // Act
        await coordinator.InsertNode(target, NodeAddOption.Last, newGrandchild);
        
        // Assert
        Assert.HasCount(3, target.Children);
        Assert.AreEqual(2, target.Children[2].MyIndex);
        
        AssertSiblingOffsets(target);
        AssertBinaryDataIntegrity();
    }

    #endregion

    #region 4. Node Insertion (InsertNode) - WITH Length Encoding Growth

    [TestMethod]
    public async Task InsertNode_Before_CausesParentLengthGrowth() {
        // Arrange
        Byte[] child0 = CreateOctetStringWithPayloadLength(126);
        Byte[] root = CreateSequence(child0);
        await coordinator.InitializeFromRawData(root);
        
        Byte[] newChild = CreateNull(); // 2 bytes
        Int32 firstChildInitialOffset = coordinator.Root!.Children[0].Value.Offset;
        
        // Act
        await coordinator.InsertNode(coordinator.Root.Children[0], NodeAddOption.Before, newChild);
        
        // Assert
        Assert.AreEqual(128, coordinator.Root.Value.PayloadLength);
        Assert.AreEqual(2, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength));
        Assert.AreEqual(firstChildInitialOffset + 1, coordinator.Root.Children[0].Value.Offset,
            "New first child offset accounts for length encoding growth");
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task InsertNode_After_MultipleSiblingsUpdated_WithLengthGrowth() {
        // Arrange: Root with 5 children, PayloadLength = 126
        Byte[][] children = Enumerable.Range(0, 5)
            .Select(i => CreateInteger((Byte)i))
            .ToArray();
        // Pad to reach 126 bytes
        Byte[] padding = CreateOctetStringWithPayloadLength(126 - children.Sum(c => c.Length));
        Byte[][] allChildren = children.Append(padding).ToArray();
        Byte[] root = CreateSequence(allChildren);
        
        await coordinator.InitializeFromRawData(root);
        
        Byte[] newChild = CreateNull(); // 2 bytes -> total 128
        
        // Act
        await coordinator.InsertNode(coordinator.Root!.Children[2], NodeAddOption.After, newChild);
        
        // Assert
        Assert.AreEqual(128, coordinator.Root.Value.PayloadLength);
        
        // Children after insertion point should have shifted
        AssertSiblingOffsets(coordinator.Root);
        AssertBinaryDataIntegrity();
    }

    #endregion

    #region 5. Node Removal (RemoveNode) - No Length Encoding Shrinkage

    [TestMethod]
    public async Task RemoveNode_LastChild_NoLengthShrinkage() {
        // Arrange
        Byte[] root = CreateSequence(CreateInteger(1), CreateInteger(2));
        await coordinator.InitializeFromRawData(root);
        
        Int32 initialPayloadLength = coordinator.Root!.Value.PayloadLength;
        AsnTreeNode nodeToRemove = coordinator.Root.Children[1];
        Int32 nodeLength = nodeToRemove.Value.TagLength;
        
        // Act
        coordinator.RemoveNode(nodeToRemove);
        
        // Assert
        Assert.HasCount(1, coordinator.Root.Children);
        Assert.AreEqual(initialPayloadLength - nodeLength, coordinator.Root.Value.PayloadLength);
        Assert.AreEqual(1, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength));
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task RemoveNode_MiddleChild_SiblingsShift() {
        // Arrange
        Byte[] root = CreateSequence(
            CreateInteger(1),
            CreateInteger(2),
            CreateInteger(3),
            CreateInteger(4),
            CreateInteger(5)
        );
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode nodeToRemove = coordinator.Root!.Children[2]; // Remove child[2]
        Int32 nodeLength = nodeToRemove.Value.TagLength;
        Int32 child3InitialOffset = coordinator.Root.Children[3].Value.Offset;
        Int32 child4InitialOffset = coordinator.Root.Children[4].Value.Offset;
        
        // Act
        coordinator.RemoveNode(nodeToRemove);
        
        // Assert
        Assert.HasCount(4, coordinator.Root.Children);
        Assert.AreEqual(child3InitialOffset - nodeLength, coordinator.Root.Children[2].Value.Offset,
            "Child[3] (now child[2]) should shift by -nodeLength");
        Assert.AreEqual(child4InitialOffset - nodeLength, coordinator.Root.Children[3].Value.Offset,
            "Child[4] (now child[3]) should shift by -nodeLength");
        
        // Verify MyIndex updated
        for (Int32 i = 0; i < coordinator.Root.Children.Count; i++) {
            Assert.AreEqual(i, coordinator.Root.Children[i].MyIndex);
        }
        
        AssertSiblingOffsets(coordinator.Root);
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task RemoveNode_NodeWithSubtree_EntireSubtreeRemoved() {
        // Arrange
        Byte[] grandchild1 = CreateInteger(1);
        Byte[] grandchild2 = CreateInteger(2);
        Byte[] grandchild3 = CreateInteger(3);
        Byte[] child = CreateSequence(grandchild1, grandchild2, grandchild3);
        Byte[] root = CreateSequence(child, CreateInteger(99));
        
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode nodeToRemove = coordinator.Root!.Children[0]; // Remove child (has 3 grandchildren)
        Int32 nodeLength = nodeToRemove.Value.TagLength;
        Int32 siblingInitialOffset = coordinator.Root.Children[1].Value.Offset;
        
        // Act
        coordinator.RemoveNode(nodeToRemove);
        
        // Assert
        Assert.HasCount(1, coordinator.Root.Children);
        Assert.AreEqual(siblingInitialOffset - nodeLength, coordinator.Root.Children[0].Value.Offset);
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task RemoveNode_FromDeepTree_AncestorsAndSiblingsUpdated() {
        // Arrange: Root -> Child[0] -> Grandchildren[3], Child[1]
        Byte[] grandchild1 = CreateInteger(1);
        Byte[] grandchild2 = CreateInteger(2);
        Byte[] grandchild3 = CreateInteger(3);
        Byte[] child0 = CreateSequence(grandchild1, grandchild2, grandchild3);
        Byte[] child1 = CreateInteger(99);
        Byte[] root = CreateSequence(child0, child1);
        
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode nodeToRemove = coordinator.Root!.Children[0].Children[1]; // grandchild[1]
        Int32 nodeLength = nodeToRemove.Value.TagLength;
        Int32 grandchild3InitialOffset = coordinator.Root.Children[0].Children[2].Value.Offset;
        Int32 child1InitialOffset = coordinator.Root.Children[1].Value.Offset;
        Int32 initialChild0Length = coordinator.Root.Children[0].Value.PayloadLength;
        Int32 initialRootLength = coordinator.Root.Value.PayloadLength;
        
        // Act
        coordinator.RemoveNode(nodeToRemove);
        
        // Assert
        Assert.HasCount(2, coordinator.Root.Children[0].Children);
        Assert.AreEqual(grandchild3InitialOffset - nodeLength,
            coordinator.Root.Children[0].Children[1].Value.Offset,
            "Grandchild[3] (now [1]) should shift");
        Assert.AreEqual(child1InitialOffset - nodeLength,
            coordinator.Root.Children[1].Value.Offset,
            "Child[1] should shift");
        Assert.AreEqual(initialChild0Length - nodeLength,
            coordinator.Root.Children[0].Value.PayloadLength,
            "Child[0] payload should decrease");
        Assert.AreEqual(initialRootLength - nodeLength,
            coordinator.Root.Value.PayloadLength,
            "Root payload should decrease");
        
        AssertBinaryDataIntegrity();
    }

    #endregion

    #region 6. Node Removal (RemoveNode) - WITH Length Encoding Shrinkage

    [TestMethod]
    public async Task RemoveNode_RootLengthShrinkage_2To1Bytes_128To127() {
        // Arrange: Root(130) with children totaling 130 bytes
        Byte[] child1 = CreateSequence(CreateOctetStringWithPayloadLength(125)); // Large child
        Byte[] child2 = CreateInteger(99); // 3 bytes
        // Total: 128 + 2 = 130 bytes
        Byte[] root = CreateSequence(child1, child2);
        
        await coordinator.InitializeFromRawData(root);
        
        Assert.AreEqual(130, coordinator.Root!.Value.PayloadLength);
        Assert.AreEqual(2, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength));
        
        AsnTreeNode nodeToRemove = coordinator.Root.Children[0]; // Remove 128-byte child
        Int32 child2InitialOffset = coordinator.Root.Children[1].Value.Offset;
        
        // Act
        coordinator.RemoveNode(nodeToRemove);
        
        // Assert
        Assert.HasCount(1, coordinator.Root.Children);
        // New payload = 3, which requires 1 byte for length
        Assert.AreEqual(3, coordinator.Root.Value.PayloadLength);
        Assert.AreEqual(1, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength));
        
        // Child2 should shift by: -(removed node) + (length shrink)
        // = -(128) + (-1) = -129
        // But wait, child2 itself moved positions, need to recalculate
        Assert.AreEqual(coordinator.Root.Value.Offset + coordinator.Root.Value.HeaderLength,
            coordinator.Root.Children[0].Value.Offset);
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task RemoveNode_MidLevelLengthShrinkage_2To1Bytes() {
        // Arrange: Root -> Child(130 bytes) -> Grandchildren
        Byte[] grandchild1 = CreateSequence(CreateOctetStringWithPayloadLength(125));
        Byte[] grandchild2 = CreateInteger(99); // 3 bytes
        Byte[] child = CreateSequence(grandchild1, grandchild2); // Total payload = 130
        Byte[] sibling = CreateInteger(88);
        Byte[] root = CreateSequence(child, sibling);
        
        await coordinator.InitializeFromRawData(root);
        
        Assert.AreEqual(130, coordinator.Root!.Children[0].Value.PayloadLength);
        Assert.AreEqual(2, GetLengthEncodingSize(coordinator.Root.Children[0].Value.PayloadLength));
        
        Int32 siblingInitialOffset = coordinator.Root.Children[1].Value.Offset;
        AsnTreeNode nodeToRemove = coordinator.Root.Children[0].Children[0]; // Remove large grandchild
        Int32 nodeLength = nodeToRemove.Value.TagLength;
        
        // Act
        coordinator.RemoveNode(nodeToRemove);
        
        // Assert
        Assert.HasCount(1, coordinator.Root.Children[0].Children);
        // Child payload = 3, length encoding shrinks from 2 to 1
        Assert.AreEqual(3, coordinator.Root.Children[0].Value.PayloadLength);
        Assert.AreEqual(1, GetLengthEncodingSize(coordinator.Root.Children[0].Value.PayloadLength));
        
        // Sibling should shift by: -nodeLength (128) -1 (length shrink) = -129
        Assert.AreEqual(siblingInitialOffset - 128 - 1,
            coordinator.Root.Children[1].Value.Offset);
        
        // Root payload should decrease by 129
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task RemoveNode_RootLengthShrinkage_3To2Bytes_256To255() {
        // Arrange: Create root with payload = 257, then remove 3-byte child
        Byte[] largeChild = CreateOctetStringWithPayloadLength(254);
        Byte[] smallChild = CreateInteger(99); // 3 bytes
        Byte[] root = CreateSequence(largeChild, smallChild); // Total = 257
        
        await coordinator.InitializeFromRawData(root);
        
        Assert.AreEqual(257, coordinator.Root!.Value.PayloadLength);
        Assert.AreEqual(3, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength));
        
        AsnTreeNode nodeToRemove = coordinator.Root.Children[1];
        
        // Act
        coordinator.RemoveNode(nodeToRemove);
        
        // Assert
        Assert.AreEqual(254, coordinator.Root.Value.PayloadLength);
        Assert.AreEqual(2, GetLengthEncodingSize(coordinator.Root.Value.PayloadLength),
            "Length encoding should shrink from 3 to 2 bytes");
        
        AssertBinaryDataIntegrity();
    }

    #endregion

    #region 7. Node Update (UpdateNode) - Same Size

    [TestMethod]
    public async Task UpdateNode_LeafNode_SameSize_OnlyContentChanges() {
        // Arrange
        Byte[] root = CreateSequence(CreateInteger(10), CreateInteger(20));
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode nodeToUpdate = coordinator.Root!.Children[0];
        Int32 initialOffset = nodeToUpdate.Value.Offset;
        Int32 child1InitialOffset = coordinator.Root.Children[1].Value.Offset;
        
        Byte[] newData = CreateInteger(99); // Same size, different value
        
        // Act
        coordinator.UpdateNode(nodeToUpdate, newData);
        
        // Assert
        Assert.AreEqual(initialOffset, nodeToUpdate.Value.Offset, "Offset should not change");
        Assert.AreEqual(child1InitialOffset, coordinator.Root.Children[1].Value.Offset,
            "Sibling offset should not change");
        Assert.AreEqual("99", nodeToUpdate.Value.ExplicitValue, "Value should update");
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task UpdateNode_StringNode_SameSize_OnlyContentChanges() {
        // Arrange
        Byte[] root = CreateSequence(CreateString("ABCD"), CreateInteger(1));
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode nodeToUpdate = coordinator.Root!.Children[0];
        Byte[] newData = CreateString("EFGH"); // Same length
        
        Int32 siblingInitialOffset = coordinator.Root.Children[1].Value.Offset;
        
        // Act
        coordinator.UpdateNode(nodeToUpdate, newData);
        
        // Assert
        Assert.AreEqual(siblingInitialOffset, coordinator.Root.Children[1].Value.Offset);
        Assert.AreEqual("EFGH", nodeToUpdate.Value.ExplicitValue);
        
        AssertBinaryDataIntegrity();
    }

    #endregion

    #region 8. Node Update (UpdateNode) - Size Increase, No L Growth

    [TestMethod]
    public async Task UpdateNode_IncreasesPayload_NoLengthGrowth() {
        // Arrange: Root(50) -> INTEGER(1 byte), INTEGER(1 byte)
        Byte[] root = CreateSequence(CreateInteger(1), CreateInteger(2));
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode nodeToUpdate = coordinator.Root!.Children[0];
        Byte[] newData = CreateInteger(0x01, 0x02, 0x03); // 5 bytes total (tag + len + 3 value)
        
        Int32 initialRootLength = coordinator.Root.Value.PayloadLength;
        Int32 child1InitialOffset = coordinator.Root.Children[1].Value.Offset;
        Int32 sizeDiff = newData.Length - nodeToUpdate.Value.TagLength;
        
        // Act
        coordinator.UpdateNode(nodeToUpdate, newData);
        
        // Assert
        Assert.AreEqual(initialRootLength + sizeDiff, coordinator.Root.Value.PayloadLength);
        Assert.AreEqual(child1InitialOffset + sizeDiff, coordinator.Root.Children[1].Value.Offset,
            "Sibling should shift by size difference");
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task UpdateNode_InDeepTree_SizeIncrease() {
        // Arrange: Root -> Child -> Grandchild (INTEGER 1 byte)
        Byte[] grandchild = CreateInteger(1);
        Byte[] child = CreateSequence(grandchild, CreateInteger(2));
        Byte[] root = CreateSequence(child);
        
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode nodeToUpdate = coordinator.Root!.Children[0].Children[0];
        Byte[] newData = CreateInteger(0x01, 0x02, 0x03, 0x04, 0x05); // 7 bytes total
        
        Int32 initialChildLength = coordinator.Root.Children[0].Value.PayloadLength;
        Int32 initialRootLength = coordinator.Root.Value.PayloadLength;
        Int32 sizeDiff = newData.Length - nodeToUpdate.Value.TagLength;
        
        // Act
        coordinator.UpdateNode(nodeToUpdate, newData);
        
        // Assert
        Assert.AreEqual(initialChildLength + sizeDiff, 
            coordinator.Root.Children[0].Value.PayloadLength);
        Assert.AreEqual(initialRootLength + sizeDiff,
            coordinator.Root.Value.PayloadLength);
        
        AssertBinaryDataIntegrity();
    }

    #endregion

    #region 9. Node Update (UpdateNode) - Size Increase, WITH L Growth

    [TestMethod]
    public async Task UpdateNode_CausesParentLengthGrowth_1To2Bytes() {
        // Arrange: Root(126) -> INTEGER(1 byte)
        Byte[] padding = CreateOctetStringWithPayloadLength(125); // 125 bytes total
        Byte[] printableString = CreateString(String.Empty); // 2 bytes
        Byte[] root = CreateSequence(padding, printableString); // Total = 126, but we need 128 after Act
        await  coordinator.InitializeFromRawData(root);
        var rootNode = coordinator.Root!;

        // pre-flight assertions
        Assert.AreEqual(127, rootNode.Value.PayloadLength);
        Assert.AreEqual(2, rootNode.Children[0].Value.Offset);
        Assert.AreEqual(127, rootNode.Children[1].Value.Offset);

        // Act
        coordinator.UpdateNode(rootNode.Children[1], CreateString("a"));

        // Assert
        Assert.AreEqual(128, rootNode.Value.PayloadLength);
        Assert.AreEqual(3, rootNode.Children[0].Value.Offset);
        Assert.AreEqual(128, rootNode.Children[1].Value.Offset);
    }

    #endregion

    #region 10. Node Update (UpdateNode) - Size Decrease

    [TestMethod]
    public async Task UpdateNode_DecreasesPayload_NoLengthShrinkage() {
        // Arrange
        Byte[] largeInt = CreateInteger(0x01, 0x02, 0x03, 0x04, 0x05); // 7 bytes
        Byte[] root = CreateSequence(largeInt, CreateInteger(1));
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode nodeToUpdate = coordinator.Root!.Children[0];
        Byte[] newData = CreateInteger(0x01, 0x02); // 4 bytes
        
        Int32 initialRootLength = coordinator.Root.Value.PayloadLength;
        Int32 child1InitialOffset = coordinator.Root.Children[1].Value.Offset;
        Int32 sizeDiff = newData.Length - nodeToUpdate.Value.TagLength; // negative
        
        // Act
        coordinator.UpdateNode(nodeToUpdate, newData);
        
        // Assert
        Assert.AreEqual(initialRootLength + sizeDiff, coordinator.Root.Value.PayloadLength);
        Assert.AreEqual(child1InitialOffset + sizeDiff, coordinator.Root.Children[1].Value.Offset);
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task UpdateNode_CausesParentLengthShrinkage_2To1Bytes() {
        // Arrange: Root(126) -> INTEGER(1 byte)
        Byte[] padding = CreateOctetStringWithPayloadLength(125); // 125 bytes total
        Byte[] printableString = CreateString("a"); // 3 bytes
        Byte[] root = CreateSequence(padding, printableString); // Total = 128, but we need 126
        await coordinator.InitializeFromRawData(root);
        var rootNode = coordinator.Root!;

        Assert.AreEqual(128, rootNode.Value.PayloadLength);
        Assert.AreEqual(3, rootNode.Children[0].Value.Offset);
        Assert.AreEqual(128, rootNode.Children[1].Value.Offset);

        // Act
        coordinator.UpdateNode(rootNode.Children[1], CreateString(String.Empty));

        // Assert
        Assert.AreEqual(127, rootNode.Value.PayloadLength);
        Assert.AreEqual(2, rootNode.Children[0].Value.Offset);
        Assert.AreEqual(127, rootNode.Children[1].Value.Offset);
    }

    #endregion

    #region 11. Edge Cases

    [TestMethod]
    public async Task AddNode_EmptyTree_BecomesRoot() {
        // Arrange
        Byte[] nodeData = CreateInteger(42);
        
        // Act
        AsnTreeNode result = await coordinator.AddNode(nodeData, null);
        
        // Assert
        Assert.IsNotNull(coordinator.Root);
        Assert.AreSame(coordinator.Root, result);
        Assert.HasCount(nodeData.Length, coordinator.RawData);
        Assert.IsTrue(result.IsRoot);
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task RemoveNode_Root_TreeReset() {
        // Arrange
        Byte[] root = CreateInteger(42);
        await coordinator.InitializeFromRawData(root);
        
        // Act
        coordinator.RemoveNode(coordinator.Root!);
        
        // Assert
        Assert.IsNull(coordinator.Root);
        Assert.HasCount(0, coordinator.RawData);
    }

    [TestMethod]
    public async Task UpdateNode_ToSameValue_NoChanges() {
        // Arrange
        Byte[] nodeData = CreateInteger(42);
        Byte[] root = CreateSequence(nodeData);
        await coordinator.InitializeFromRawData(root);
        
        AsnTreeNode nodeToUpdate = coordinator.Root!.Children[0];
        Byte[] sameData = CreateInteger(42);
        
        Int32 initialOffset = nodeToUpdate.Value.Offset;
        Int32 initialRootLength = coordinator.Root.Value.PayloadLength;
        
        // Act
        coordinator.UpdateNode(nodeToUpdate, sameData);
        
        // Assert
        Assert.AreEqual(initialOffset, nodeToUpdate.Value.Offset);
        Assert.AreEqual(initialRootLength, coordinator.Root.Value.PayloadLength);
        
        AssertBinaryDataIntegrity();
    }

    [TestMethod]
    public async Task AddNode_NullParent_NonRootNode_ThrowsException() {
        // Arrange
        Byte[] root = CreateInteger(1);
        await coordinator.InitializeFromRawData(root);

        Byte[] nodeData = CreateInteger(2);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await coordinator.AddNode(nodeData, null),
            "Should throw ArgumentNullException when parent is null for non-root node"
        );
    }

    [TestMethod]
    public void UpdateNode_NullNode_ThrowsException() {
        // Arrange
        Byte[] nodeData = CreateInteger(1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => coordinator.UpdateNode(null!, nodeData),
            "Should throw ArgumentNullException when node parameter is null"
        );
    }

    #endregion
}

/// <summary>
/// Mock implementation of INodeViewOptions for testing.
/// </summary>
class NodeViewOptionsMock : INodeViewOptions {
    public event PropertyChangedEventHandler? PropertyChanged;
    public Boolean ShowContent { get; set; } = true;
    public Boolean ShowInHex { get; set; }
    public Boolean ShowNodeLength { get; set; } = true;
    public Boolean ShowNodeOffset { get; set; } = true;
    public Boolean ShowNodePath { get; set; }
    public Boolean ShowTagNumber { get; set; }
    
    public IAsnIntegerViewOptions GetIntegerViewOptions() {
        return new IntegerViewOptionsMock();
    }
    
    public IAsnDateTimeViewOptions GetDateTimeViewOptions() {
        return new DateTimeViewOptionsMock();
    }

    class IntegerViewOptionsMock : IAsnIntegerViewOptions {
        public Boolean IntegerAsInteger { get; set; } = true;
    }

    class DateTimeViewOptionsMock : IAsnDateTimeViewOptions {
        public Boolean UseISO8601Format { get; set; }
    }
}
